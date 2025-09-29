﻿using ChatApi.Core.Abstracts.ServicesContracts;
using ChatApi.Core.Enums.ChatEnums;
using ChatApi.Core.Features.Chat.Commands.RequestsModels;
using ChatApi.Core.Features.Chat.Queries.RequestsModels;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApi.Hubs {

    [Authorize]
    public class ChatHub : Hub {
        private readonly IConnectionService _connectionService;
        private readonly IMediator _mediator;
        private readonly IChatService _chatService;
        private readonly ICurrentUserService _currentUserService;

        public ChatHub(IConnectionService connectionService, IMediator mediator, IChatService chatService, ICurrentUserService currentUserService) {
            _connectionService = connectionService;
            _mediator = mediator;
            _chatService = chatService;
            _currentUserService = currentUserService;
        }

        #region Connection Management
        public override async Task OnConnectedAsync() {
            var isOnlineBeforeAddConnection = await _currentUserService.IsOnline();

            var userId = _currentUserService.UserId;
            if (userId is not null) {
                await _connectionService.AddUserConnectionAsync(userId.Value, Context.ConnectionId);
                var response = await _mediator.Send(new GetUserConversationsQuery());
                foreach (var convo in response.Data) {
                    await _connectionService.AddToGroupAsync(Context.ConnectionId, convo.Id);
                    await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{convo.Id}");

                }
                if (!isOnlineBeforeAddConnection) {
                    foreach (var con in response.Data)
                        await Clients.Group($"Conversation_{con.Id}").SendAsync("UserOnlineStatusChanged", userId, true);
                }



            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception) {
            var userId = _currentUserService.UserId;
            await _connectionService.RemoveUserConnectionAsync(Context.ConnectionId);

            if (userId is not null) {
                var isOnline = await _currentUserService.IsOnline();
                if (!isOnline) {
                    await Clients.Others.SendAsync("UserOnlineStatusChanged", userId, false);
                }
            }

            var isOnlineAfterRemoveConnection = await _currentUserService.IsOnline();
            if (isOnlineAfterRemoveConnection) {
                var userConversations = await _chatService.GetUserConversationsAsync(userId.Value);
                foreach (var con in userConversations)
                    await Clients.Group($"Conversation_{con.Id}").SendAsync("UserOnlineStatusChanged", userId, false);

            }

            await base.OnDisconnectedAsync(exception);
        }

        public async Task JoinConversation(int conversationId) {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
            await _connectionService.AddToGroupAsync(Context.ConnectionId, conversationId);
        }

        public async Task LeaveConversation(int conversationId) {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
            await _connectionService.RemoveFromGroupAsync(Context.ConnectionId, $"Conversation_{conversationId}");
        }
        #endregion

        #region Conversation Management
        public async Task CreateConversation(List<int> participantIds, string? title, ConversationType? type) {
            var command = new CreateConversationCommand {
                Title = title,
                Type = type ?? ConversationType.Direct,
                ParticipantIds = participantIds
            };

            var response = await _mediator.Send(command);

            if (response.Succeeded) {
                var conversationData = response.Data;
                if (conversationData != null) {
                    foreach (var participant in conversationData.Participants) {
                        var connections = await _connectionService.GetUserConnectionsAsync(participant.UserId);
                        foreach (var connectionId in connections)
                            await Groups.AddToGroupAsync(connectionId, $"Conversation_{conversationData.Id}");
                    }
                    if (type == ConversationType.Group)
                        await Clients.Group($"Conversation_{conversationData.Id}").SendAsync("NewGroupCreated", conversationData);
                    else
                        await Clients.Caller.SendAsync("NewDirectConversationInfo", conversationData);

                }
                else {
                    // Fallback to original behavior if data is not available
                    var userId = _currentUserService.UserId!.Value;
                    var allParticipants = new List<int>(participantIds);
                    if (!allParticipants.Contains(userId))
                        allParticipants.Add(userId);

                    var participantConnections = new List<string>();
                    foreach (var participantId in allParticipants) {
                        var connections = await _connectionService.GetUserConnectionsAsync(participantId);
                        participantConnections.AddRange(connections);
                    }

                    await Clients.Clients(participantConnections).SendAsync("ConversationCreated", new {
                        Id = 0, // This won't work properly now
                        Title = title,
                        Type = type,
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        ParticipantIds = allParticipants
                    });
                }
            }
            else {
                await Clients.Caller.SendAsync("Error", response.Message);
            }
        }

        public async Task AddParticipant(int conversationId, int userIdToAdd, ConversationParticipantRole role = ConversationParticipantRole.Member) {
            var command = new AddParticipantCommand {
                ConversationId = conversationId,
                UserId = userIdToAdd,
                Role = role
            };

            var response = await _mediator.Send(command);

            if (response.Succeeded) {
                // Notify conversation members about new participant
                await Clients.Group($"Conversation_{conversationId}")
                    .SendAsync("ParticipantAdded", conversationId, userIdToAdd, role);

                // Notify the new participant
                var newParticipantConnections = await _connectionService.GetUserConnectionsAsync(userIdToAdd);
                await Clients.Clients(newParticipantConnections)
                    .SendAsync("AddedToConversation", conversationId);
            }
            else {
                await Clients.Caller.SendAsync("Error", response.Message);
            }
        }

        public async Task RemoveParticipant(int conversationId, int userIdToRemove) {
            var command = new RemoveParticipantCommand {
                ConversationId = conversationId,
                UserId = userIdToRemove
            };

            var response = await _mediator.Send(command);

            if (response.Succeeded) {
                // Notify conversation members about removed participant
                await Clients.Group($"Conversation_{conversationId}")
                    .SendAsync("ParticipantRemoved", conversationId, userIdToRemove);

                // Notify the removed participant
                var removedParticipantConnections = await _connectionService.GetUserConnectionsAsync(userIdToRemove);
                await Clients.Clients(removedParticipantConnections)
                    .SendAsync("RemovedFromConversation", conversationId);
            }
            else {
                await Clients.Caller.SendAsync("Error", response.Message);
            }
        }
        #endregion

        #region Message Management
        public async Task SendMessage(SendMessageCommand command) {
            var response = await _mediator.Send(command);
            if (response.Succeeded) {
                var messageData = response.Data;
                await Clients.Group($"Conversation_{messageData?.ConversationId}")
                    .SendAsync("ReceiveMessage", messageData);

            }
            else {
                await Clients.Caller.SendAsync("Error", response.Message);
            }
        }

        public async Task EditMessage(int messageId, string newContent) {
            var command = new EditMessageCommand {
                MessageId = messageId,
                NewContent = newContent
            };

            var response = await _mediator.Send(command);

            if (response.Succeeded) {
                // Note: We need to get conversation ID to target the right group
                // This should be improved by returning more data from the command
                await Clients.All.SendAsync("MessageEdited", new {
                    MessageId = messageId,
                    NewContent = newContent,
                    EditedAt = DateTime.UtcNow
                });
            }
            else {
                await Clients.Caller.SendAsync("Error", response.Message);
            }
        }

        public async Task DeleteMessage(int messageId) {
            var command = new DeleteMessageCommand { MessageId = messageId };

            var response = await _mediator.Send(command);

            if (response.Succeeded) {
                // Note: Should target specific conversation
                await Clients.All.SendAsync("MessageDeleted", messageId);
            }
            else {
                await Clients.Caller.SendAsync("Error", response.Message);
            }
        }

        public async Task MarkMessageAsRead(int messageId) {
            var command = new MarkMessageAsReadCommand {
                MessageId = messageId
            };

            var response = await _mediator.Send(command);
            if (response.Succeeded) {
                var userId = _currentUserService.UserId!.Value;
                // Note: Should notify sender specifically
                await Clients.All.SendAsync("MessageRead", messageId, userId);
            }
        }
        #endregion

        #region Typing Indicators
        public async Task StartTyping(int conversationId) {
            var command = new UpdateTypingStatusCommand {
                ConversationId = conversationId,
                IsTyping = true
            };

            var response = await _mediator.Send(command);

            if (response.Succeeded) {
                var userId = _currentUserService.UserId!.Value;
                await Clients.OthersInGroup($"Conversation_{conversationId}")
                    .SendAsync("UserStartedTyping", conversationId, userId);
            }
        }

        public async Task StopTyping(int conversationId) {
            var command = new UpdateTypingStatusCommand {
                ConversationId = conversationId,
                IsTyping = false
            };

            var response = await _mediator.Send(command);

            if (response.Succeeded) {
                var userId = _currentUserService.UserId!.Value;
                await Clients.OthersInGroup($"Conversation_{conversationId}")
                    .SendAsync("UserStoppedTyping", conversationId, userId);
            }
        }
        #endregion

        #region Helper Methods
        //private int GetCurrentUserId() {
        //    var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        //}

        private async Task AddAllUserConnectionsToGroup(int userId, string groupName) {
            var userConnections = await _connectionService.GetUserConnectionsAsync(userId);
            foreach (var connectionId in userConnections)
                await Groups.AddToGroupAsync(connectionId, groupName);
        }
    }


    #endregion
}


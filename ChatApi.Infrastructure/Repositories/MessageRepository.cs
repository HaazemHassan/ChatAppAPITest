using ChatApi.Core.Abstracts.InfrastructureAbstracts;
using ChatApi.Core.Entities.ChatEntities;
using ChatApi.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace ChatApi.Infrastructure.Repositories {
    public class MessageRepository : GenericRepository<Message>, IMessageRepository {
        public MessageRepository(AppDbContext dbContext) : base(dbContext) { }

        public async Task<IEnumerable<Message>> GetConversationMessagesAsync(int conversationId, int skip = 0, int take = 50) {
            return await _dbContext.Messages
                .Where(m => m.ConversationId == conversationId && !m.IsDeleted)
                .Include(m => m.Sender)
                .Include(m => m.ReplyToMessage)
                    .ThenInclude(r => r.Sender)
                .OrderBy(m => m.SentAt)
                .Skip(skip)
                .Take(take)
                .ToListAsync();
        }

        public async Task<Message?> GetMessageWithRepliesAsync(int messageId) {
            return await _dbContext.Messages
                .Include(m => m.Sender)
                .Include(m => m.Replies)
                    .ThenInclude(r => r.Sender)
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);
        }

        public async Task<Message?> GetMessageWithSenderAsync(int messageId) {
            return await _dbContext.Messages
                .Include(m => m.Sender)
                .Include(m => m.Conversation)
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);
        }
    }
}
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using System.Linq;

namespace DiscordBot.Services
{
    public class RoleService
    {
        private Settings.Deserialized.Settings Settings;
        private ILoggingService _logging;
        public RoleService(Settings.Deserialized.Settings Settings, ILoggingService logging)
        {
            this.Settings = Settings;
            _logging = logging;
        }


        public Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            ReactionAddedHandlerAsync(arg1, arg2, arg3);
            return Task.CompletedTask;
        }

        private async Task ReactionAddedHandlerAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {

            if (arg2.Id == Settings.RoleAddingChannel.Id)
            {
                ITextChannel channel = arg2 as ITextChannel;
                IUserMessage msg = arg1.HasValue ? arg1.Value : channel.GetMessageAsync(arg1.Id) as IUserMessage;
                IGuildUser user = await channel.GetUserAsync(arg3.UserId);
                if (!msg.MentionedRoleIds.Any())
                {
                    await channel.DeleteMessageAsync(msg);
                    return;
                }
                IRole role = channel.Guild.GetRole(msg.MentionedRoleIds.First());
                if (Settings.AllRoles.Roles.Contains(role.Name))
                {
                    await user.AddRoleAsync(role);
                    await _logging.LogAction($"{user.Username} has added role {role} to himself in {channel.Name}");
                }
                else await channel.DeleteMessageAsync(msg);
            }
        }
        public Task ReactionRemovedHandler(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {
            ReactionRemovedHandlerAsync(arg1, arg2, arg3);
            return Task.CompletedTask;
        }

        private async Task ReactionRemovedHandlerAsync(Cacheable<IUserMessage, ulong> arg1, ISocketMessageChannel arg2, SocketReaction arg3)
        {

            if (arg2.Id == Settings.RoleAddingChannel.Id)
            {
                ITextChannel channel = arg2 as ITextChannel;
                IUserMessage msg = arg1.HasValue ? arg1.Value : channel.GetMessageAsync(arg1.Id) as IUserMessage;
                IGuildUser user = await channel.GetUserAsync(arg3.UserId);
                
                if (!msg.MentionedRoleIds.Any())
                {
                    await channel.DeleteMessageAsync(msg);
                    return;
                }
                IRole role = channel.Guild.GetRole(msg.MentionedRoleIds.First());
                if (Settings.AllRoles.Roles.Contains(role.Name))
                {
                    await user.RemoveRoleAsync(role);
                    await _logging.LogAction($"{user.Username} has added role {role} to himself in {channel.Name}");
                }
                else await channel.DeleteMessageAsync(msg);
            }
        }

        public async Task UpdateChannel(IGuild guild)
        {
            ITextChannel channel = await guild.GetTextChannelAsync(Settings.RoleAddingChannel.Id);
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, OverwritePermissions.DenyAll(channel));
            IEmote reaction = null;
            if (Emote.TryParse(Settings.RoleAddingReaction, out Emote emote))
            {
                reaction = emote;
            }
            else
            {
                reaction = new Emoji(Settings.RoleAddingReaction);
            }
            var messages = await channel.GetMessagesAsync().FlattenAsync();
            foreach(string str in Settings.AllRoles.Roles)
            {
                IRole role = guild.Roles.First(x => x.Name == str);
                if(!messages.Any(x=>x.MentionedRoleIds.First() == role.Id))
                {
                    bool wasMentionable = role.IsMentionable;
                    await role.ModifyAsync(x => x.Mentionable = true);
                    await channel.SendMessageAsync(role.Mention);
                    await role.ModifyAsync(x => x.Mentionable = wasMentionable);
                }
            }
            await channel.AddPermissionOverwriteAsync(guild.EveryoneRole, new OverwritePermissions(sendMessages:PermValue.Deny, addReactions:PermValue.Deny));
        }
    }
}

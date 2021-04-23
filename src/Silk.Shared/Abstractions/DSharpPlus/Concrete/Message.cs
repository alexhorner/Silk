﻿using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Silk.Shared.Abstractions.DSharpPlus.Interfaces;

namespace Silk.Shared.Abstractions.DSharpPlus.Concrete
{
    public class Message : IMessage
    {
        private readonly DiscordMessage _message;
        public Message(DiscordMessage message)
        {
            _message = message;

            Id = message.Id;
            GuildId = message.Channel.GuildId;
            Author = (User) message.Author;
            Content = message.Content;
            Timestamp = message.CreationTimestamp;
            Reply = (Message) message.ReferencedMessage!;
            Reactions = default!;
        }

        public ulong Id { get; }

        public ulong? GuildId { get; }

        public IUser Author { get; }

        public string? Content { get; private set; }

        public DateTimeOffset Timestamp { get; }

        //public IEmbed Embed { get; }

        public IMessage? Reply { get; }

        public IReadOnlyCollection<IEmoji> Reactions { get; }

        public async Task CreateReactionAsync(ulong emojiId)
        {
            var client = _message
                .GetType()
                .BaseType!
                .GetProperty("Discord", BindingFlags.NonPublic | BindingFlags.Instance)!
                .GetValue(_message) as DiscordClient;

            var emoji = DiscordEmoji.FromGuildEmote(client, emojiId);
            await _message.CreateReactionAsync(emoji);
        }

        public async Task DeleteAsync()
        {
            try
            {
                await _message.DeleteAsync();
            }
            catch (NotFoundException)
            {
                /* Ignored. */
            }
        }

        public async Task EditAsync(string content)
        {
            Content = content;
            await _message.ModifyAsync(m => m.Content = content);
        }

        public static explicit operator Message?(DiscordMessage? message)
        {
            if (message is null) return null;

            return new(message);
        }
    }
}
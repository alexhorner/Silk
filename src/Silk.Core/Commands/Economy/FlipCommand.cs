﻿using System;
using System.Threading.Tasks;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Silk.Core.Database.Models;
using Silk.Core.Services.Interfaces;

namespace Silk.Core.Commands.Economy
{
    public class FlipCommand : BaseCommandModule
    {
        private readonly IDatabaseService _dbService;
        private readonly string[] _winningMessages = new[]
        {
            "Capitalism shines upon you.",
            "RNG is having a good day, or would it be a bad day?",
            "You defeated all odds, but how far does that luck stretch?",
            "Double the money, but half the luck~ Just kidding ;p"
        };
        private readonly string[] _losingMessages = new[]
        {
            "Yikes!",
            "Better luck next time!",
            "RNG is not your friend, now is it?",
            "Alas, the cost of gambling.",
            "Hope that wasn't all your earnings"
        };


        public FlipCommand(IDatabaseService dbService)
        {
            _dbService = dbService;
        }
        
        [Command]
        [Description("Flip a metaphorical coin, and double your profits, or lose everything~")]
        public async Task FlipAsync(CommandContext ctx, uint amount)
        {
            DiscordMessageBuilder builder = new();
            DiscordEmbedBuilder embedBuilder = new();
            builder.WithReply(ctx.Message.Id);
            
            GlobalUser user = await _dbService.GetOrCreateGlobalUserAsync(ctx.User.Id);

            if (amount > user.Cash)
            {
                builder.WithContent("Ah ah ah... You can't gamble more than what you've got in your pockets!");
                await ctx.RespondAsync(builder);
                return;
            }
            
            Random ran = new((int)ctx.Message.Id);
            bool won;

            int nextRan = ran.Next(10000);

            won = nextRan % 20 > 2;

            if (won)
            {
                embedBuilder.WithColor(DiscordColor.SapGreen)
                    .WithTitle(_winningMessages[ran.Next(_winningMessages.Length)])
                    .WithDescription($"Congragulations! You've doubled your money {user.Cash} -> {user.Cash * 2}");
                builder.WithEmbed(embedBuilder);
                user.Cash *= 2;
                await ctx.RespondAsync(builder);
                await _dbService.UpdateGlobalUserAsync(user);
            }
            else
            {
                embedBuilder.WithColor(DiscordColor.SapGreen)
                    .WithTitle(_losingMessages[ran.Next(_losingMessages.Length)])
                    .WithDescription($"Darn. Seems like you've lost your bet! {user.Cash} -> {user.Cash - amount}");
                builder.WithEmbed(embedBuilder);
                user.Cash -= (int)amount;
                await ctx.RespondAsync(builder);
                await _dbService.UpdateGlobalUserAsync(user);
            }
        }
    }
}
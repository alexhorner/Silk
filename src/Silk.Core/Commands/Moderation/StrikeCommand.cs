﻿using System.Linq;
using System.Threading.Tasks;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using DSharpPlus.Interactivity.Extensions;
using Humanizer;
using MediatR;
using Silk.Core.Data.MediatR.Infractions;
using Silk.Core.Data.Models;
using Silk.Core.Services.Interfaces;
using Silk.Core.Types;
using Silk.Core.Utilities;

namespace Silk.Core.Commands.Moderation
{
	public class StrikeCommand : BaseCommandModule
	{
		private readonly IInfractionService _infractionHelper;
		private readonly IMediator _mediator;
		public StrikeCommand(IInfractionService infractionHelper, IMediator mediator)
		{
			_infractionHelper = infractionHelper;
			_mediator = mediator;
		}

		[Command("strike")]
		[Aliases("warn", "w", "bonk")]
		[RequireFlag(UserFlag.Staff)]
		public async Task Strike(CommandContext ctx, DiscordMember user, [RemainingText] string reason = "Not Given.")
		{
			var esclated = await CheckForEscalationAsync(ctx, user, reason);
			var result = await _infractionHelper.StrikeAsync(user.Id, ctx.Guild.Id, ctx.User.Id, reason, esclated);
			
			var response = result switch
			{
				InfractionResult.SucceededWithNotification => "Successfully warned user (Notified with Direct Message)",
				InfractionResult.SucceededWithoutNotification => "Successfully warned user (Failed to Direct Message)",
				InfractionResult.FailedLogPermissions => "Successfully warned user (Failed to Log).",
				_ => $"The command returned a result that I'm not sure how to respond to! If it makes any sense to you, the response was: {result.Humanize(LetterCasing.Sentence)}"
			};
			await ctx.RespondAsync(response);
		}
		
		private async Task<bool> CheckForEscalationAsync(CommandContext ctx, DiscordUser user, string reason)
		{
			var infractions = await _mediator.Send(new GetUserInfractionsRequest(ctx.Guild.Id, user.Id));
			var interactivity = ctx.Client.GetInteractivity();
			
			if (infractions.Count(inf => inf.Type != InfractionType.Note) < 6)
				return false;

			var currentStep = await _infractionHelper.GetCurrentInfractionStepAsync(ctx.Guild.Id, infractions);
			var currentStepType = currentStep.Type is InfractionType.Ignore ? InfractionType.Ban : currentStep.Type;
			var builder = new DiscordMessageBuilder()
				.WithContent("User has 5 or more infractions on record. Would you like to escalate?")
				.AddComponents(
					new DiscordButtonComponent(ButtonStyle.Success, $"escalate_{ctx.Message.Id}", $"Esclate to {currentStepType.Humanize(LetterCasing.Sentence)}", emoji: new("✔")),
					new DiscordButtonComponent(ButtonStyle.Danger, $"do_not_escalate_{ctx.Message.Id}", "Do not escalate", emoji: new("❌")));

			var msg = await ctx.RespondAsync(builder);
			var res = await interactivity.WaitForButtonAsync(msg, ctx.User);

			if (res.TimedOut)
				return false;

			var escalated = res.Result.Id.StartsWith("escalate");
			await res.Result.Interaction.CreateResponseAsync(InteractionResponseType.UpdateMessage, new() {Content = escalated ? "Got it." : "Proceeding with strike."});
			_ = TimedDelete();
			return escalated;

			async Task TimedDelete()
			{
				await Task.Delay(3000);
				await msg!.DeleteAsync();
			}
		}
	}
}
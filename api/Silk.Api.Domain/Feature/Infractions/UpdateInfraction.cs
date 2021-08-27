﻿using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Silk.Api.Data;
using Silk.Api.Data.Models;

namespace Silk.Api.Domain.Feature.Infractions
{
	public static class UpdateInfraction
	{
		public record Request : IRequest<ApiModel>
		{
			public Guid Key { get; init; }
			public InfractionType? Type { get; init; }
			public string? Reason { get; init; }
			public bool? IsPardoned { get; init; }
		}

		public record ApiModel(bool Changed);

		public class Handler : IRequestHandler<Request, ApiModel>
		{
			private readonly ApiContext _db;
			public Handler(ApiContext db) => _db = db;
			
			public async Task<ApiModel> Handle(Request request, CancellationToken cancellationToken)
			{
				if ((!request.IsPardoned ?? true) && request.Reason is null && request.Type is null)
					return new(false);

				var entity = await _db.Infractions.FirstOrDefaultAsync(i => i.Key == request.Key, cancellationToken);
				
				if (entity is null)
					return null;
				
				entity.Reason = request.Reason ?? entity.Reason;
				entity.IsPardoned = request.IsPardoned ?? entity.IsPardoned;
				entity.Type = request.Type ?? entity.Type;
				
				
				var updated = await _db.SaveChangesAsync(cancellationToken);

				if (updated is 0)
					return new(false);

				entity.Updated = DateTime.UtcNow;

				await _db.SaveChangesAsync(cancellationToken);
				
				return new(true);
			}
		}

		public class Validator : AbstractValidator<Request>
		{
			public Validator()
			{
				RuleFor(r => r.Reason)
					.MaximumLength(4000)
					.WithMessage("New reason must be less than 4000 characters.");

				RuleFor(r => r.IsPardoned)
					.Must(r => r ?? false)
					.Unless(r => !r.IsPardoned.HasValue)
					.WithMessage("Infractions cannot be unpardoned.");
				
			}
		}
	}
}
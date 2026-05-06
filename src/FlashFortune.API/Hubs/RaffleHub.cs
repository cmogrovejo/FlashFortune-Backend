using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace FlashFortune.API.Hubs;

/// <summary>
/// Real-time hub for the Draw Arena.
/// The server pushes events — clients never call methods that affect state here.
/// All state mutations go through the REST controllers + MediatR pipeline.
/// </summary>
[Authorize]
public sealed class RaffleHub : Hub
{
    // Groups are named by raffleId so only connected clients for that raffle receive events.
    public async Task JoinRaffle(string raffleId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"raffle:{raffleId}");
    }

    public async Task LeaveRaffle(string raffleId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"raffle:{raffleId}");
    }
}

// Hub event contracts (used by controllers to push events):
//
// "DrawStarted"     { raffleId, prizeId, prizeName }
// "WinnerFound"     { raffleId, prizeId, winnerId, fullName, identityDocPartial, accountNumber, couponNumber, resultHash }
// "ExclusionSkipped"{ raffleId, skippedCoupon, reason }
// "PanicReset"      { raffleId, prizeId }

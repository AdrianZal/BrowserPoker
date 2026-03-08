using Microsoft.EntityFrameworkCore;
using Poker.Models;
using Poker.Models.DTOs;

namespace Poker.Services
{
    public class UtilService(PokerContext context)
    {
        public async Task<OwnedSkinsDTO> GetOwnedSkinsAsync(string playerName)
        {
            var playerId = await context.Players
                .Where(p => p.Name == playerName)
                .Select(p => p.Id)
                .FirstOrDefaultAsync();

            var ownedFileNames = await context.PlayerOwnedReverseSkins
                .Where(pos => pos.PlayerId == playerId)
                .Select(x => x.Skin.Filename)
                .ToListAsync();

            var equippedSkin = await context.PlayerEquippedReverseSkins
                .Where(x => x.PlayerId == playerId)
                .Select(x => x.PlayerOwnedReverseSkin.Skin.Filename)
                .FirstOrDefaultAsync();

            return new OwnedSkinsDTO
            {
                Skins = ownedFileNames.ToDictionary
                (
                    filename => filename,
                    filename => filename == equippedSkin
                )
            };
        }

        public async Task ChangeEquippedSkinAsync(string playerName, string fileName)
        {
            var ownedSkin = await context.PlayerOwnedReverseSkins
                .Where(os => os.Player.Name == playerName && os.Skin.Filename == fileName)
                .Select(os => new { os.PlayerId, os.SkinId })
                .FirstOrDefaultAsync();

            if (ownedSkin == null) return;

            var currentEquipped = await context.PlayerEquippedReverseSkins
                .FirstOrDefaultAsync(e => e.PlayerId == ownedSkin.PlayerId);

            if (currentEquipped != null)
            {
                currentEquipped.SkinId = ownedSkin.SkinId;
            }
            else
            {
                return;
            }

            await context.SaveChangesAsync();
        }

        public async Task<int> GetBalanceAsync(string playerName)
        {
            var playerBalance = await context.Players
                .Where(p => p.Name == playerName)
                .Select(p => p.Balance)
                .FirstOrDefaultAsync();

            return playerBalance;
        }

        public async Task<string> OpenCase(string playerName)
        {
            var playerCase = await context.PlayerCases
                .Include(pc => pc.Player)
                .FirstOrDefaultAsync(pc => pc.Player.Name == playerName);

            if (playerCase == null || playerCase.Number <= 0)
            {
                return string.Empty;
            }

            var randomSkin = await context.CardReverseSkins
                .OrderBy(s => EF.Functions.Random())
                .FirstOrDefaultAsync();

            if (randomSkin == null) return string.Empty;

            playerCase.Number--;

            var alreadyOwned = await context.PlayerOwnedReverseSkins
                .AnyAsync(ors => ors.PlayerId == playerCase.PlayerId && ors.SkinId == randomSkin.Id);

            if (!alreadyOwned)
            {
                context.PlayerOwnedReverseSkins.Add(new PlayerOwnedReverseSkin
                {
                    PlayerId = playerCase.PlayerId,
                    SkinId = randomSkin.Id
                });
            }

            await context.SaveChangesAsync();

            return randomSkin.Filename;
        }

        public async Task<int> GetPlayerCasesCount(string playerName)
        {
            return await context.PlayerCases
                .Where(pc => pc.Player.Name == playerName)
                .Select(pc => pc.Number)
                .FirstOrDefaultAsync();
        }

        public async Task AddCaseToPlayer(string playerName)
        {
            var playerCase = await context.PlayerCases
                .Include(pc => pc.Player)
                .FirstOrDefaultAsync(pc => pc.Player.Name == playerName);
            if (playerCase != null)
            {
                playerCase.Number ++;
                await context.SaveChangesAsync();
            }
        }

    }
}

using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace Poker.Models;

public partial class PokerContext : DbContext
{
    public PokerContext() { }

    public PokerContext(DbContextOptions<PokerContext> options) : base(options) { }

    public virtual DbSet<Card> Cards { get; set; }
    public virtual DbSet<CardFrontSkin> CardFrontSkins { get; set; }
    public virtual DbSet<CardReverseSkin> CardReverseSkins { get; set; }
    public virtual DbSet<Player> Players { get; set; }
    public virtual DbSet<PlayerEquippedFaceSkin> PlayerEquippedFaceSkins { get; set; }
    public virtual DbSet<PlayerEquippedReverseSkin> PlayerEquippedReverseSkins { get; set; }
    public virtual DbSet<PlayerOwnedFaceSkin> PlayerOwnedFaceSkins { get; set; }
    public virtual DbSet<PlayerOwnedReverseSkin> PlayerOwnedReverseSkins { get; set; }
    public virtual DbSet<PlayerTable> PlayerTables { get; set; }
    public virtual DbSet<RefreshToken> RefreshTokens { get; set; }
    public virtual DbSet<Table> Tables { get; set; }
    public virtual DbSet<PlayerCases> PlayerCases { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Card>(entity =>
        {
            entity.HasKey(e => e.Card1).HasName("Card_pk");
            entity.ToTable("Card");
            entity.Property(e => e.Card1).HasColumnName("card");
        });

        modelBuilder.Entity<CardFrontSkin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CardFrontSkin_pk");
            entity.ToTable("CardFrontSkin");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Card).HasColumnName("card");
            entity.Property(e => e.Filename).HasColumnName("filename");
            entity.Property(e => e.Name).HasColumnName("name");

            entity.HasOne(d => d.CardNavigation).WithMany(p => p.CardFrontSkins)
                .HasForeignKey(d => d.Card)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("CardFrontSkin_Card");
        });

        modelBuilder.Entity<CardReverseSkin>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("CardReverseSkin_pk");
            entity.ToTable("CardReverseSkin");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Filename).HasColumnName("filename");
            entity.Property(e => e.Name).HasColumnName("name");
        });

        modelBuilder.Entity<Player>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("Player_pk");
            entity.ToTable("Player");

            entity.HasIndex(e => e.Email, "UQ_Player_Email").IsUnique();
            entity.HasIndex(e => e.Name, "UQ_Player_Name").IsUnique();

            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.Balance).HasColumnName("balance");
            entity.Property(e => e.Email).HasColumnName("email");
            entity.Property(e => e.Name).HasColumnName("name");
            entity.Property(e => e.Password).HasColumnName("password");
        });

        modelBuilder.Entity<PlayerEquippedFaceSkin>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.Card }).HasName("PlayerEquippedFaceSkin_pk");
            entity.ToTable("PlayerEquippedFaceSkin");

            entity.Property(e => e.PlayerId).HasColumnName("Player_id");
            entity.Property(e => e.Card).HasColumnName("card");
            entity.Property(e => e.SkinId).HasColumnName("Skin_id");

            entity.HasOne(d => d.CardNavigation)
                .WithMany(p => p.PlayerEquippedFaceSkins)
                .HasForeignKey(d => d.Card)
                .HasPrincipalKey(p => p.Card1)
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("PlayerEquippedFaceSkin_Card");

            entity.HasOne(d => d.PlayerOwnedFaceSkin)
                .WithMany(p => p.PlayerEquippedFaceSkins)
                .HasForeignKey(d => new { d.PlayerId, d.SkinId })
                .OnDelete(DeleteBehavior.Cascade)
                .HasConstraintName("PlayerEquippedFaceSkin_PlayerOwnedFaceSkin");
        });

        modelBuilder.Entity<PlayerEquippedReverseSkin>(entity =>
        {
            entity.HasKey(e => e.PlayerId).HasName("PlayerEquippedReverseSkin_pk");
            entity.ToTable("PlayerEquippedReverseSkin");

            entity.Property(e => e.PlayerId).ValueGeneratedNever().HasColumnName("Player_id");
            entity.Property(e => e.SkinId).HasColumnName("Skin_id");

            entity.HasOne(d => d.PlayerOwnedReverseSkin).WithMany(p => p.PlayerEquippedReverseSkins)
                .HasForeignKey(d => new { d.PlayerId, d.SkinId })
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PlayerOwnedFaceSkin>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.SkinId }).HasName("PlayerOwnedFaceSkin_pk");
            entity.ToTable("PlayerOwnedFaceSkin");
            entity.Property(e => e.PlayerId).HasColumnName("Player_id");
            entity.Property(e => e.SkinId).HasColumnName("Skin_id");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerOwnedFaceSkins).HasForeignKey(d => d.PlayerId);
            entity.HasOne(d => d.Skin).WithMany(p => p.PlayerOwnedFaceSkins).HasForeignKey(d => d.SkinId);
        });

        modelBuilder.Entity<PlayerOwnedReverseSkin>(entity =>
        {
            entity.HasKey(e => new { e.PlayerId, e.SkinId }).HasName("PlayerOwnedReverseSkin_pk");
            entity.ToTable("PlayerOwnedReverseSkin");
            entity.Property(e => e.PlayerId).HasColumnName("Player_id");
            entity.Property(e => e.SkinId).HasColumnName("Skin_id");

            entity.HasOne(d => d.Player).WithMany(p => p.PlayerOwnedReverseSkins).HasForeignKey(d => d.PlayerId);
            entity.HasOne(d => d.Skin).WithMany(p => p.PlayerOwnedReverseSkins).HasForeignKey(d => d.SkinId);
        });

        modelBuilder.Entity<PlayerTable>(entity =>
        {
            entity.HasKey(e => e.PlayerId).HasName("PlayerTable_pk");
            entity.ToTable("PlayerTable");
            entity.Property(e => e.PlayerId).ValueGeneratedNever().HasColumnName("Player_id");
            entity.Property(e => e.TableBalance).HasColumnName("table_balance");
            entity.Property(e => e.TableJoinCode).HasColumnName("Table_joinCode");

            entity.HasOne(d => d.Player).WithOne(p => p.PlayerTable).HasForeignKey<PlayerTable>(d => d.PlayerId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("RefreshToken_pk");
            entity.ToTable("RefreshToken");
            entity.Property(e => e.Id).HasColumnName("id");
            entity.Property(e => e.ExpiresAt).HasColumnName("expires_at");
            entity.Property(e => e.PlayerId).HasColumnName("Player_id");
            entity.Property(e => e.Revoked).HasColumnName("revoked");
            entity.Property(e => e.TokenHash).HasColumnName("token_hash");
        });

        modelBuilder.Entity<Table>(entity =>
        {
            entity.HasKey(e => e.JoinCode).HasName("Table_pk");
            entity.ToTable("Table");
            entity.Property(e => e.JoinCode).HasColumnName("join_code");
            entity.Property(e => e.BuyIn).HasColumnName("buy_in");
        });
        modelBuilder.Entity<PlayerCases>(entity =>
        {
            entity.HasKey(e => e.PlayerId).HasName("PlayerCase_pk");

            entity.ToTable("PlayerCases");

            entity.Property(e => e.PlayerId)
                .ValueGeneratedNever()
                .HasColumnName("Player_id");

            entity.Property(e => e.Number)
                .HasColumnName("number");

            entity.HasOne(d => d.Player)
                .WithOne(p => p.PlayerCases)
                .HasForeignKey<PlayerCases>(d => d.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
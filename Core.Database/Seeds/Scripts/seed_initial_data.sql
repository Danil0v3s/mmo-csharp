-- Initial seed data extracted from rathena main.sql
-- This file contains reference data needed for the game to function

-- ============================================
-- Clan Data
-- ============================================
INSERT INTO `clan` (clan_id, name, master, mapname, max_member) VALUES (1, 'Swordman Clan', 'Raffam Oranpere', 'prontera', 500);
INSERT INTO `clan` (clan_id, name, master, mapname, max_member) VALUES (2, 'Arcwand Clan', 'Devon Aire', 'geffen', 500);
INSERT INTO `clan` (clan_id, name, master, mapname, max_member) VALUES (3, 'Golden Mace Clan', 'Berman Aire', 'prontera', 500);
INSERT INTO `clan` (clan_id, name, master, mapname, max_member) VALUES (4, 'Crossbow Clan', 'Shaam Rumi', 'payon', 500);

-- ============================================
-- Clan Alliance Data
-- ============================================
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (1, 0, 3, 'Golden Mace Clan');
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (2, 0, 3, 'Golden Mace Clan');
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (2, 1, 4, 'Crossbow Clan');
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (3, 0, 1, 'Swordman Clan');
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (3, 0, 2, 'Arcwand Clan');
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (3, 0, 4, 'Crossbow Clan');
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (4, 0, 3, 'Golden Mace Clan');
INSERT INTO `clan_alliance` (clan_id, opposition, alliance_id, name) VALUES (4, 1, 2, 'Arcwand Clan');

-- ============================================
-- Default Server Account
-- ============================================
-- INSECURE: Change password in production!
INSERT INTO `login` (account_id, userid, user_pass, sex, email) VALUES ('1', 's1', 'p1', 'S', 'athena@athena.com');


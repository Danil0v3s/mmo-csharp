-- Initial seed data extracted from rathena main.sql
-- This file contains reference data needed for the game to function

-- ============================================
-- Clan Data
-- ============================================
INSERT INTO `clan` VALUES (1, 'Swordman Clan', 'Raffam Oranpere', 'prontera', 500);
INSERT INTO `clan` VALUES (2, 'Arcwand Clan', 'Devon Aire', 'geffen', 500);
INSERT INTO `clan` VALUES (3, 'Golden Mace Clan', 'Berman Aire', 'prontera', 500);
INSERT INTO `clan` VALUES (4, 'Crossbow Clan', 'Shaam Rumi', 'payon', 500);

-- ============================================
-- Clan Alliance Data
-- ============================================
INSERT INTO `clan_alliance` VALUES (1, 0, 3, 'Golden Mace Clan');
INSERT INTO `clan_alliance` VALUES (2, 0, 3, 'Golden Mace Clan');
INSERT INTO `clan_alliance` VALUES (2, 1, 4, 'Crossbow Clan');
INSERT INTO `clan_alliance` VALUES (3, 0, 1, 'Swordman Clan');
INSERT INTO `clan_alliance` VALUES (3, 0, 2, 'Arcwand Clan');
INSERT INTO `clan_alliance` VALUES (3, 0, 4, 'Crossbow Clan');
INSERT INTO `clan_alliance` VALUES (4, 0, 3, 'Golden Mace Clan');
INSERT INTO `clan_alliance` VALUES (4, 1, 2, 'Arcwand Clan');

-- ============================================
-- Default Server Account
-- ============================================
-- INSECURE: Change password in production!
insert into login (account_id, userid, user_pass, sex, email, group_id, state, unban_time, expiration_time, logincount, lastlogin, last_ip, birthdate, character_slots, pincode, pincode_change, vip_time, old_group, web_auth_token, web_auth_token_enabled) values (1, 's1', 'p1', 'S', 'athena@athena.com', 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);


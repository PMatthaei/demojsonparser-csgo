﻿using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DemoInfo;
using Newtonsoft.Json;
using demojsonparser.src.JSON.objects;
using demojsonparser.src.JSON.events;
using demojsonparser.src.JSON.objects.subobjects;

namespace demojsonparser.src.JSON
{
    class JSONParser
    {
        enum Eventtype { SMOKENADE, SMOKENADE_ENDED, FLASH, FLASH_ENDED, HEGRENADE, DECOY, DECOY_ENDED };

        private static StreamWriter outputStream;
        private DemoParser parser;

        enum PlayerType {META, NORMAL, DETAILED, WITHEQUIPMENT };

        public JSONParser(DemoParser parser, string path)
        {
            this.parser = parser;
            string outputpath = path.Replace(".dem", "") + ".json";
            outputStream = new StreamWriter(outputpath);
        }
        /// <summary>
        /// Dumps the Gamestate in prettyjson or as one-liner(default)
        /// </summary>
        /// <param name="gs"></param>
        /// <param name="prettyjson"></param>
        public void dump(JSONGamestate gs, bool prettyjson)
        {
            Formatting f = Formatting.None;
            if (prettyjson)
                f = Formatting.Indented;

            outputStream.Write(JsonConvert.SerializeObject(gs, f));
            //gs = null;
        }

        /// <summary>
        /// Dumps a string to the json
        /// </summary>
        /// <param name="s"></param>
        public void dump(string s)
        {
            outputStream.Write(s);
        }

        public void stopParser()
        {
            outputStream.Close();
            parser = null;
        }


        public JSONGamemeta assembleGamemeta()
        {
            return new JSONGamemeta
            {
                gamestate_id = 0,
                mapname = parser.Map,
                tickrate = parser.TickRate,
                players = assemblePlayers(parser.PlayingParticipants),
            };
        }


        #region Gameevents

        public JSONPlayerKilled assemblePlayerKilled(PlayerKilledEventArgs pke)
        {
            return new JSONPlayerKilled
            {
                gameevent = "player_killed",
                attacker = assemblePlayerDetailed(pke.Killer),
                victim = assemblePlayerDetailed(pke.Victim),
                headhshot = pke.Headshot,
                penetrated = pke.PenetratedObjects,
                hitgroup = 0,
                weapon = assembleWeapon(pke.Weapon)
            };
        }

        public JSONWeaponFire assembleWeaponFire(WeaponFiredEventArgs we)
        {
            return new JSONWeaponFire
            {
                gameevent = "weapon_fire",
                shooter = assemblePlayerDetailed(we.Shooter),
                weapon = assembleWeapon(we.Weapon)
            };
        }

        public JSONPlayerHurt assemblePlayerHurt(PlayerHurtEventArgs phe)
        {
            JSONPlayerHurt ph = new JSONPlayerHurt
            {
                gameevent = "player_hurt",
                attacker = assemblePlayer(phe.Attacker),
                victim = assemblePlayer(phe.Player),
                armor = phe.Armor,
                armor_damage = phe.ArmorDamage,
                HP = phe.Health,
                HP_damage = phe.HealthDamage,
                hitgroup = phe.Hitgroup.ToString(),
                weapon = assembleWeapon(phe.Weapon)
            };
            return ph;
        }

        public JSONPlayerFootstep assemblePlayerFootstep(Player p)
        {
            return new JSONPlayerFootstep
            {
                gameevent = "player_footstep",
                player = assemblePlayer(p)
            };
        }

        #region Nades

        public JSONNade assembleNade(NadeEventArgs e, string eventname)
        {
            Player[] ps = null;

            if (e.GetType() == typeof(FlashEventArgs)) //Exception for FlashEvents -> we need flashed players
            {
                FlashEventArgs f = e as FlashEventArgs;
                ps = f.FlashedPlayers;
                return new JSONFlashNade
                {
                    gameevent = eventname,
                    thrownby = assemblePlayer(e.ThrownBy),
                    nadetype = e.NadeType.ToString(),
                    position = new JSONPosition3D { x = e.Position.X, y = e.Position.Y, z = e.Position.Z },
                    flashedplayers = assemblePlayers(f.FlashedPlayers)
                };
            }

            return new JSONNade
            {
                gameevent = eventname,
                thrownby = assemblePlayer(e.ThrownBy),
                nadetype = e.NadeType.ToString(),
                position = new JSONPosition3D { x = e.Position.X, y = e.Position.Y, z = e.Position.Z },
            };
        }


        #endregion

        #region Bombevents

        public JSONBomb assembleBomb(BombEventArgs be, string gameevent)
        {
            return new JSONBomb
            {
                gameevent = gameevent,
                site = be.Site,
                player = assemblePlayer(be.Player)
            };
        }

        public JSONBomb assembleBombDefuse(BombDefuseEventArgs bde, string gameevent)
        {
            return new JSONBomb
            {
                gameevent = gameevent,
                haskit = bde.HasKit,
                player = assemblePlayer(bde.Player)
            };
        }
        #endregion

        #endregion



        #region SUBEVENTS
        
        public List<JSONPlayer> assemblePlayers(Player[] ps)
        {
            if (ps == null)
                return null;
            List<JSONPlayer> players = new List<JSONPlayer>();
            foreach (var player in ps)
                players.Add(assemblePlayer(player));

            return players;
        }

        public List<JSONPlayerMeta> assemblePlayers(IEnumerable<Player> ps)
        {
            if (ps == null)
                return null;
            List<JSONPlayerMeta> players = new List<JSONPlayerMeta>();
            foreach (var player in ps)
                players.Add(assemblePlayerMeta(player));

            return players;
        }


        public JSONPlayer assemblePlayer(Player p)
        {
            JSONPlayer player = new JSONPlayer
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new JSONPosition3D { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new JSONFacing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString()
            };
            return player;
        }

        public JSONPlayerMeta assemblePlayerMeta(Player p)
        {
            JSONPlayerMeta player = new JSONPlayerMeta
            {
                playername = p.Name,
                player_id = p.EntityID,
                team = p.Team.ToString(),
                clanname = p.AdditionaInformations.Clantag,
                steam_id = p.SteamID
            
            };
            return player;
        }

        public JSONPlayerDetailed assemblePlayerDetailed(Player p)
        {
            JSONPlayerDetailed playerdetailed = new JSONPlayerDetailed
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new JSONPosition3D { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new JSONFacing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString(),
                hasHelmet = p.HasHelmet,
                hasdefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = p.Velocity.Absolute //Length of Movementvector -> Velocity
            };

            return playerdetailed;
        }


        public JSONPlayerDetailedWithItems assemblePlayerDetailedWithItems(Player p)
        {
            JSONPlayerDetailedWithItems playerdetailed = new JSONPlayerDetailedWithItems
            {
                playername = p.Name,
                player_id = p.EntityID,
                position = new JSONPosition3D { x = p.Position.X, y = p.Position.Y, z = p.Position.Z },
                facing = new JSONFacing { yaw = p.ViewDirectionY, pitch = p.ViewDirectionX },
                team = p.Team.ToString(),
                hasHelmet = p.HasHelmet,
                hasdefuser = p.HasDefuseKit,
                HP = p.HP,
                armor = p.Armor,
                velocity = p.Velocity.Absolute, //Length of Movementvector -> Velocity
                items = assembleWeapons(p.Weapons)
            };

            return playerdetailed;
        }

        public List<JSONItem> assembleWeapons(IEnumerable<Equipment> wps)
        {
            List<JSONItem> jwps = new List<JSONItem>();
            foreach (var w in wps)
                jwps.Add(assembleWeapon(w));

            return jwps;
        }

        public JSONItem assembleWeapon(Equipment wp)
        {
            if (wp == null)
            {
                Console.WriteLine("Weapon null. Bytestream not suitable for this version of DemoInfo");
                return new JSONItem();
            }

            JSONItem jwp = new JSONItem
            {
                weapon = wp.Weapon.ToString(),
                ammoinmagazine = wp.AmmoInMagazine
            };

            return jwp;
        }

        #endregion

    }


}
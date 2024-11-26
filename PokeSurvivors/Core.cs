using HarmonyLib;
using Il2CppNewtonsoft.Json;
using Il2CppNewtonsoft.Json.Linq;
using Il2CppVampireSurvivors.Data;
using Il2CppVampireSurvivors.Framework;
using Il2CppVampireSurvivors.Framework.DLC;
using Il2CppVampireSurvivors.Objects;
using Il2CppVampireSurvivors.Objects.Characters;
using MelonLoader;
using System.IO.Pipes;
using UnityEngine;
using static Il2CppMono.Security.X509.X509Stores;
using static Il2CppSystem.ComponentModel.MaskedTextProvider;
using static Il2CppTMPro.SpriteAssetUtilities.TexturePacker_JsonArray;
using static MelonLoader.MelonLogger;
//using static UnityEngine.TextEditor;

[assembly: MelonInfo(typeof(PokeSurvivors.Core), "PokeSurvivors", "1.2.0", "sup3p", null)]
[assembly: MelonGame("poncle", "Vampire Survivors")]

namespace PokeSurvivors
{
    public class Core : MelonMod
    {
        public static MelonLogger.Instance melonLog;
        public static Il2CppSystem.Collections.Generic.Dictionary<string, Il2CppSystem.Collections.Generic.List<Sprite>> animationSprites = new();
        private static Vector2 defaultPivot = new(0.5f, 0);
        private static readonly CharacterType startingCharType = (CharacterType)3501;
        private static CharacterType charType = startingCharType;
        private static Il2CppVampireSurvivors.Objects.Characters.CharacterController characterController;
        public static Dictionary<string, Texture2D> characterTextures = new();
        private static Dictionary<string, CharacterType> characterTypes = new();
        enum SilvallyStats
        {
            None,
            Recovery,
            Armor,
            MoveSpeed,
            Might,
            Speed,
            Duration,
            Area,
            Cooldown,
            Amount,
            Magnet,
            Luck,
            Growth,
            Greed,
            Curse
        }
        private static readonly string[] statTypes = {"", "Fairy", "Steel", "Flying", "Fighting", "Electric", "Psychic", "Fire", "Ice", "Bug", "Water", "Grass", "Dragon", "Rock", "Ghost"};
        private static SilvallyStats currentSilvallyType = SilvallyStats.None;

        public override void OnInitializeMelon()
        {
            melonLog = LoggerInstance;
            LoggerInstance.Msg("Initialized.");
        }

        private static JArray CreateBaseCharacterData(string textureFileName, string textureName, int width, int height, int frames, string charName, string desc, string weapon)
        {
            string spriteName = charName.ToLower();

            Texture2D charTexture = ResourceHelper.CreateCharacterTexture(textureFileName, textureName);
            ResourceHelper.CreateCharacterSpritesFromStrip(charTexture, 0, 0, width, height, defaultPivot, spriteName, frames);

            JObject charData = new();
            charData["level"] = 1;
            charData["startingWeapon"] = weapon;
            charData["charName"] = charName;
            charData["textureName"] = textureName;
            charData["spriteName"] = $"{spriteName}_01";
            charData["walkingFrames"] = frames;
            charData["description"] = desc;
            charData["maxHp"] = 100;
            charData["cooldown"] = 1;
            charData["armor"] = 0;
            charData["regen"] = 0;
            charData["moveSpeed"] = 1;
            charData["power"] = 1;
            charData["area"] = 1;
            charData["speed"] = 1;
            charData["duration"] = 1;
            charData["amount"] = 0;
            charData["luck"] = 1;
            charData["growth"] = 1;
            charData["greed"] = 1;
            charData["curse"] = 1;
            charData["magnet"] = 0;
            charData["revivals"] = 0;
            charData["rerolls"] = 0;
            charData["skips"] = 0;
            charData["banish"] = 0;

            JObject defaultSkin = new();
            defaultSkin["skinType"] = "DEFAULT";
            defaultSkin["name"] = "Default";
            defaultSkin["textureName"] = textureName;
            defaultSkin["spriteName"] = $"{spriteName}_01";
            defaultSkin["walkingFrames"] = frames;
            defaultSkin["unlocked"] = true;
            JArray skinsArray = new();
            skinsArray.Add(defaultSkin);
            charData["skins"] = skinsArray;

            JArray fullCharArray = new();
            fullCharArray.Add(charData);
            fullCharArray.Add(new JObject());
            fullCharArray[1]["growth"] = 1;
            fullCharArray[1]["level"] = 20;
            fullCharArray.Add(new JObject());
            fullCharArray[2]["growth"] = 1;
            fullCharArray[2]["level"] = 40;
            fullCharArray.Add(new JObject());
            fullCharArray[3]["growth"] = -1;
            fullCharArray[3]["level"] = 21;
            fullCharArray.Add(new JObject());
            fullCharArray[4]["growth"] = -1;
            fullCharArray[4]["level"] = 41;

            return fullCharArray;
        }

        private static void AddCharacter(ref DataManager dataManager, JArray characterArray)
        {
            dataManager._allCharactersJson[charType.ToString()] = characterArray;
            string charName = characterArray[0]["charName"].ToString();
            characterTypes.Add(charName, charType);
            melonLog.Msg($"Added character {charName}");
            charType++;
        }

        [HarmonyPatch(typeof(DataManager))]
        class DataManager_Patch
        {
            [HarmonyPatch(nameof(DataManager.LoadBaseJObjects))]
            [HarmonyPostfix]
            static void LoadBaseJObjects_Postfix(DataManager __instance)
            {
                JArray workingArray;
                
                workingArray = CreateBaseCharacterData("mimikyuTexture.png", "character_mimikyu", 26, 29, 4, "Mimikyu", "Gains -4% Cooldown and +8% Area after reviving.", "SUMMONNIGHT");
                workingArray[0]["moveSpeed"] = 1.1;
                workingArray[0]["power"] = 1.1;
                workingArray[0]["banish"] = 5;
                workingArray[0]["revivals"] = 1;
                AddCharacter(ref __instance, workingArray);

                workingArray = CreateBaseCharacterData("lampentTexture.png", "character_lampent", 29, 31, 4, "Lampent", "Gains +1% Might every level.", "FIREBALL");
                workingArray[0]["area"] = 1.1;
                JObject levelUpBonus = new();
                levelUpBonus["power"] = 0.01;
                workingArray[0]["onEveryLevelUp"] = levelUpBonus;
                AddCharacter(ref __instance, workingArray);

                workingArray = CreateBaseCharacterData("silvallyTexture.png", "character_silvally", 35, 37, 5, "Silvally", "Temporarily doubles a stat each level. Stats are doubled in a set order.", "TRIASSO1");
                for (int i = 1; i <= 14; i++) 
                {
                    ResourceHelper.CreateCharacterSpritesFromStrip(characterTextures["character_silvally"], 0, 37 * i, 35, 37, defaultPivot, $"silvally{statTypes[i]}", 5);
                }
                AddCharacter(ref __instance, workingArray);
            }
        }

        [HarmonyPatch(typeof(Il2CppVampireSurvivors.Objects.Characters.CharacterController))]
        class CharacterController_Patch
        {
            [HarmonyPatch(nameof(Il2CppVampireSurvivors.Objects.Characters.CharacterController.InitCharacter))]
            [HarmonyPostfix]
            static void InitCharacter_Patch(Il2CppVampireSurvivors.Objects.Characters.CharacterController __instance, CharacterType characterType)
            {
                characterController = __instance;
                if (characterType >= startingCharType && characterType <= charType && __instance.CurrentCharacterData.GetCurrentSkinData().skinType == SkinType.DEFAULT)
                {
                    foreach (Sprite animSprite in animationSprites[__instance.CurrentCharacterData.charName.ToLower()])
                    {
                        __instance.Anims._animations["walk"]._frames.Add(animSprite);
                    }
                    __instance.SetupAnimation();
                }

               if (characterType == characterTypes["Silvally"])
                {
                    currentSilvallyType = SilvallyStats.None;
                    int fps = __instance.Anims.GetAnimation("walk")._fps;
                    foreach (string silvallyType in statTypes)
                    {
                        if (silvallyType != "")
                        {
                            __instance.Anims.AddAnimation($"walk{silvallyType}", animationSprites[$"silvally{silvallyType}"], fps, true, autoSetAnimation: false);
                        }
                    }
                }
            }

            [HarmonyPatch(nameof(Il2CppVampireSurvivors.Objects.Characters.CharacterController.Revive))]
            [HarmonyPostfix]
            static void Revive_Patch(Il2CppVampireSurvivors.Objects.Characters.CharacterController __instance)
            {
                if (__instance.CharacterType == characterTypes["Mimikyu"])
                {
                    ModifierStats buff = new();
                    buff.Cooldown = -0.04f;
                    buff.Area = 0.08f;
                    __instance.PlayerStatsUpgrade(buff);
                }
            }

            [HarmonyPatch(nameof(Il2CppVampireSurvivors.Objects.Characters.CharacterController.PlayerStatsUpgrade))]
            [HarmonyPrefix]
            static void PlayerStatsUpgrade_Patch(Il2CppVampireSurvivors.Objects.Characters.CharacterController __instance, ref ModifierStats other)
            {
                //melonLog.Msg(JsonConvert.SerializeObject(other));
                if (__instance.CharacterType == characterTypes["Silvally"])
                {
                    switch (currentSilvallyType)
                    {
                        case SilvallyStats.Recovery: { other.Regen *= 2; break; }
                        case SilvallyStats.Armor: { other.Armor *= 2; break; }
                        case SilvallyStats.MoveSpeed: { other.MoveSpeed *= 2; break; }
                        case SilvallyStats.Might: { other.Power *= 2; break; }
                        case SilvallyStats.Speed: { other.Speed *= 2; break; }
                        case SilvallyStats.Duration: { other.Duration *= 2; break; }
                        case SilvallyStats.Area: { other.Area *= 2; break; }
                        case SilvallyStats.Cooldown: { other.Cooldown *= 0.5f; break; }
                        case SilvallyStats.Amount: { other.Amount *= 2; break; }
                        case SilvallyStats.Magnet: { other.Magnet *= 2; break; }
                        case SilvallyStats.Luck: { other.Luck *= 2; break; }
                        case SilvallyStats.Growth: { other.Growth *= 2; break; }
                        case SilvallyStats.Greed: { other.Greed *= 2; break; }
                        //case SilvallyStats.Curse: { other.Curse *= 2; break; }
                    }
                }
            }

            [HarmonyPatch(nameof(Il2CppVampireSurvivors.Objects.Characters.CharacterController.LevelUp))]
            [HarmonyPrefix]
            static void LevelUp_Patch(Il2CppVampireSurvivors.Objects.Characters.CharacterController __instance)
            {
                //melonLog.Msg(__instance.PRegen());
                
                if (__instance.CharacterType == characterTypes["Silvally"])
                {
                    ModifierStats doubleStat = new();
                    if (currentSilvallyType != SilvallyStats.None)
                    {
                        switch (currentSilvallyType)
                        {
                            case SilvallyStats.Recovery: { doubleStat.Regen = __instance.PRegen() * -0.5f; break; }
                            case SilvallyStats.Armor: { doubleStat.Armor = __instance.PArmor() * -0.5f; break; }
                            case SilvallyStats.MoveSpeed: { doubleStat.MoveSpeed = __instance.PMoveSpeed() * -0.5f; break; }
                            case SilvallyStats.Might: { doubleStat.Power = __instance.PPower() * -0.5f; break; }
                            case SilvallyStats.Speed: { doubleStat.Speed = __instance.PSpeed() * -0.5f; break; }
                            case SilvallyStats.Duration: { doubleStat.Duration = __instance.PDuration() * -0.5f; break; }
                            case SilvallyStats.Area: { doubleStat.Area = __instance.PArea() * -0.5f; break; }
                            case SilvallyStats.Cooldown: { doubleStat.Cooldown = __instance.PCooldown(); break; }
                            case SilvallyStats.Amount: { doubleStat.Amount = __instance.PAmount() * -0.5f; break; }
                            case SilvallyStats.Magnet: { doubleStat.Magnet = -1; break; }
                            case SilvallyStats.Luck: { doubleStat.Luck = __instance.PLuck() * -0.5f; break; }
                            case SilvallyStats.Growth: { doubleStat.Growth = __instance.PGrowth() * -0.5f; break; }
                            case SilvallyStats.Greed: { doubleStat.Greed = __instance.PGreed() * -0.5f; break; }
                            case SilvallyStats.Curse: { doubleStat.Curse = __instance.PCurse() * -0.5f; break; }
                        }
                    }
                    if (currentSilvallyType == SilvallyStats.Greed)
                    {
                        currentSilvallyType = SilvallyStats.Recovery;
                    }
                    else
                    {
                        currentSilvallyType++;
                        if (currentSilvallyType == SilvallyStats.Magnet) { currentSilvallyType++; }
                    }
                    switch (currentSilvallyType)
                    {
                        case SilvallyStats.Recovery: { doubleStat.Regen = __instance.PRegen() / 2; break; }
                        case SilvallyStats.Armor: { doubleStat.Armor = __instance.PArmor() / 2; break; }
                        case SilvallyStats.MoveSpeed: { doubleStat.MoveSpeed = __instance.PMoveSpeed() / 2; break; }
                        case SilvallyStats.Might: { doubleStat.Power = __instance.PPower() / 2; break; }
                        case SilvallyStats.Speed: { doubleStat.Speed = __instance.PSpeed() / 2; break; }
                        case SilvallyStats.Duration: { doubleStat.Duration = __instance.PDuration() / 2; break; }
                        case SilvallyStats.Area: { doubleStat.Area = __instance.PArea() / 2; break; }
                        case SilvallyStats.Cooldown: { doubleStat.Cooldown = __instance.PCooldown() * -1; break; }
                        case SilvallyStats.Amount: { doubleStat.Amount = __instance.PAmount() / 2; break; }
                        case SilvallyStats.Magnet: { doubleStat.Magnet = 0.5f; break; }
                        case SilvallyStats.Luck: { doubleStat.Luck = __instance.PLuck() / 2; break; }
                        case SilvallyStats.Growth: { doubleStat.Growth = __instance.PGrowth() / 2; break; }
                        case SilvallyStats.Greed: { doubleStat.Greed = __instance.PGreed() / 2; break; }
                        case SilvallyStats.Curse: { doubleStat.Curse = __instance.PCurse() / 2; break; }
                    }
                    __instance.PlayerStatsUpgrade(doubleStat);
                    __instance.Anims.SetAnimation($"walk{statTypes[(int)currentSilvallyType]}");

                }
            }
        }
    }
}
using BepInEx;
using ComputerInterface.Extensions;
using ComputerInterface.Interfaces;
using GorillaNetworking;
using HarmonyLib;
using Photon.Pun;
using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ComputerInterface
{
    public static class BaseGameInterface
    {
        public const int MAX_ROOM_LENGTH = 10;
        public const int MAX_NAME_LENGTH = 12;

        // WordCheckResult (enum), WordCheckResultToMessage, WordAllowed
        #region Word Checking

        public enum WordCheckResult
        {
            Allowed,
            Empty,
            Blank,
            NotAllowed,
            TooLong,
            ComputerNotFound
        }

        public static string WordCheckResultToMessage(WordCheckResult result) => result switch
        {
            WordCheckResult.Allowed => "Input is allowed",
            WordCheckResult.Empty => "Input is empty",
            WordCheckResult.Blank => "Input is blank",
            WordCheckResult.NotAllowed => "Input is not allowed",
            WordCheckResult.TooLong => "Input is too long",
            WordCheckResult.ComputerNotFound => "Computer not found",
            _ => throw new ArgumentOutOfRangeException(nameof(result), result, null)
        };

        private static WordCheckResult WordAllowed(string word)
        {
            if (word.Length == 0) return WordCheckResult.Empty;
            if (string.IsNullOrWhiteSpace(word)) return WordCheckResult.Blank;
            if (!CheckForComputer(out GorillaComputer computer)) return WordCheckResult.ComputerNotFound;
            if (!computer.CheckAutoBanListForName(word)) return WordCheckResult.NotAllowed;
            if (word.Length > MAX_NAME_LENGTH) return WordCheckResult.TooLong;
            return WordCheckResult.Allowed;
        }

        #endregion

        // Disconnect, JoinRoom, GetRoomCode
        #region Room settings

        public static WordCheckResult JoinRoom(string roomId, JoinType joinType = JoinType.Solo)
        {
            WordCheckResult roomAllowed = WordAllowed(roomId);

            if (roomAllowed == WordCheckResult.Allowed)
            {
                PhotonNetworkController.Instance.AttemptToJoinSpecificRoom(roomId, joinType);
            }

            return roomAllowed;
        }

        public static void Disconnect() => NetworkSystem.Instance.ReturnToSinglePlayer();

        public static string GetRoomCode() => NetworkSystem.Instance.InRoom ? NetworkSystem.Instance.RoomName : null;

        #endregion

        // GetName, SetName
        #region Name Settings

        public static string GetName() => CheckForComputer(out GorillaComputer computer) ? computer.savedName : null;

        public static WordCheckResult SetName(string name)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return WordCheckResult.ComputerNotFound;

            WordCheckResult wordAllowed = WordAllowed(name);
            if (wordAllowed == WordCheckResult.Allowed)
            {
                name = name.Replace(" ", "");

                computer.currentName = name;
                NetworkSystem.Instance.SetMyNickName(computer.currentName);

                computer.offlineVRRigNametagText.text = name;
                computer.savedName = name;
                PlayerPrefs.SetString("playerName", name);
                PlayerPrefs.Save();

                GetColor(out float r, out float g, out float b);
                InitializeNoobMaterial(r, g, b);
            }

            return wordAllowed;
        }

        #endregion

        // SetColor, GetColor, InitializeNoobMaterial
        #region Colour Settings

        public static void SetColor(float r, float g, float b)
        {
            PlayerPrefs.SetFloat("redValue", r);
            PlayerPrefs.SetFloat("greenValue", g);
            PlayerPrefs.SetFloat("blueValue",b);

            GorillaTagger.Instance.UpdateColor(r, g, b);
            PlayerPrefs.Save();

            InitializeNoobMaterial(r, g, b);
        }
        public static void SetColor(Color color) => SetColor(color.r, color.g, color.b);

        public static void GetColor(out float r, out float g, out float b)
        {
            r = PlayerPrefs.GetFloat("redValue");
            g = PlayerPrefs.GetFloat("greenValue");
            b = PlayerPrefs.GetFloat("blueValue");
        }

        public static Color GetColor()
        {
            GetColor(out float r, out float g, out float b);
            return new Color(r, g, b);
        }

        public static void InitializeNoobMaterial(float r, float g, float b) => InitializeNoobMaterial(new Color(r, g, b));

        private static void InitializeNoobMaterial(Color color)
        {
            if (NetworkSystem.Instance.InRoom)
                GorillaTagger.Instance.myVRRig.SendRPC("InitializeNoobMaterial", RpcTarget.All, color.r, color.g, color.b);
        }

        #endregion

        // SetTurnMode, GetTurnMode, SetTurnValue, GetTurnValue
        #region Turn settings

        public static void SetTurnMode(ETurnMode turnMode)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return;

            string turnModeString = turnMode.ToString().ToUpper();
            computer.SetField("turnType", turnModeString);

            PlayerPrefs.SetString("stickTurning", turnModeString);
            PlayerPrefs.Save();

            GorillaTagger.Instance.GetComponent<GorillaSnapTurn>().ChangeTurnMode(turnModeString, computer.GetField<int>("turnValue"));
        }

        public static ETurnMode GetTurnMode()
        {
            string turnMode = PlayerPrefs.GetString("stickTurning");
            if (turnMode.IsNullOrWhiteSpace()) return ETurnMode.None;
            return (ETurnMode)Enum.Parse(typeof(ETurnMode), string.Concat(turnMode.ToUpper()[1], turnMode.ToLower()[1..]));
        }

        public static void SetTurnValue(int value)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return;

            computer.SetField("turnValue", value);
            PlayerPrefs.SetInt("turnFactor", value);
            PlayerPrefs.Save();
            GorillaTagger.Instance.GetComponent<GorillaSnapTurn>().ChangeTurnMode(computer.GetField<string>("turnType"), value);
        }

        public static int GetTurnValue() => PlayerPrefs.GetInt("turnFactor", 4);

        #endregion

        // SetInstrumentVolume, GetInstrumentVolume, SetItemMode, GetItemMode
        #region Item settings

        public static void SetInstrumentVolume(int value)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return;

            computer.instrumentVolume = value / 50f;
            PlayerPrefs.SetFloat("instrumentVolume", computer.instrumentVolume);
            PlayerPrefs.Save();
        }

        public static float GetInstrumentVolume() => PlayerPrefs.GetFloat("instrumentVolume", 0.1f);

        public static void SetItemMode(bool disableParticles)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return;

            computer.disableParticles = disableParticles;
            PlayerPrefs.SetString("disableParticles", disableParticles ? "TRUE" : "FALSE");
            PlayerPrefs.Save();
            GorillaTagger.Instance.ShowCosmeticParticles(!disableParticles);
        }

        public static bool GetItemMode() => PlayerPrefs.GetString("disableParticles") == "TRUE";

        #endregion

        // SetPttMode, GetPttMode
        #region Microphone settings

        public static void SetPttMode(EPTTMode mode)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return;

            string modeString = mode switch
            {
                EPTTMode.AllChat => "ALL CHAT",
                EPTTMode.PushToTalk => "PUSH TO TALK",
                EPTTMode.PushToMute => "PUSH TO MUTE",
                _ => throw new ArgumentOutOfRangeException()
            };

            computer.pttType = modeString;
            PlayerPrefs.SetString("pttType", modeString);
            PlayerPrefs.Save();
        }

        public static EPTTMode GetPttMode() => PlayerPrefs.GetString("pttType", "ALL CHAT") switch
        {
            "ALL CHAT" => EPTTMode.AllChat,
            "PUSH TO TALK" => EPTTMode.PushToTalk,
            "PUSH TO MUTE" => EPTTMode.PushToMute,
            _ => throw new ArgumentOutOfRangeException()
        };

        #endregion

        // SetVoiceMode, GetVoiceMode
        #region Voice settings

        public static void SetVoiceMode(bool humanVoiceOn)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return;

            computer.voiceChatOn = humanVoiceOn ? "TRUE" : "FALSE";
            PlayerPrefs.SetString("voiceChatOn", computer.voiceChatOn);
            PlayerPrefs.Save();

            Assembly GorillaAssembly = typeof(GorillaTagger).Assembly;
            Type ContainerType = GorillaAssembly.GetType("RigContainer");
            AccessTools.Method(ContainerType, "RefreshAllRigVoices").Invoke(null, null);
        }

        public static bool GetVoiceMode() => PlayerPrefs.GetString("voiceChatOn", "TRUE") == "TRUE";

        #endregion

        // JoinGroupMap, GetGroupJoinMaps
        #region Group settings

        public static void JoinGroupMap(int map)
        {
            if (!CheckForComputer(out GorillaComputer computer)) return;

            string[] allowedMapsToJoin = GetGroupJoinMaps();

            map = Mathf.Min(allowedMapsToJoin.Length - 1, map);

            computer.groupMapJoin = allowedMapsToJoin[map].ToUpper();
            computer.groupMapJoinIndex = map;
            PlayerPrefs.SetString("groupMapJoin", computer.groupMapJoin);
            PlayerPrefs.SetInt("groupMapJoinIndex", computer.groupMapJoinIndex);
            PlayerPrefs.Save();

            computer.OnGroupJoinButtonPress(Mathf.Min(allowedMapsToJoin.Length - 1, map), computer.friendJoinCollider);
        }

        public static string[] GetGroupJoinMaps() => CheckForComputer(out GorillaComputer computer) ? computer.allowedMapsToJoin : Array.Empty<string>();

        #endregion

        // displaySupportTab (bool)
        #region Support settings

        public static bool displaySupportTab;

        #endregion

        // SetQueue, GetQueue, AllowedInCompetitive
        #region Queue settings

        public static void SetQueue(IQueueInfo queue)
        {
            if (queue.QueueName == "COMPETITIVE" && !AllowedInCompetitive()) return;

            GorillaComputer.instance.currentQueue = queue.QueueName;
            PlayerPrefs.SetString("currentQueue", queue.QueueName);
        }

        public static string GetQueue() => PlayerPrefs.GetString("currentQueue", "DEFAULT");

        public static bool AllowedInCompetitive() => CheckForComputer(out GorillaComputer computer) && computer.allowedInCompetitive;

        #endregion

        // InitColorState, InitNameState, InitTurnState, InitMicState, InitVoiceMode, InitItemMode, InitSupportMode, InitAll
        #region Initialization

        public static void InitColorState()
        {
            GorillaTagger.Instance.UpdateColor(
                PlayerPrefs.GetFloat("redValue", 0f),
                PlayerPrefs.GetFloat("greenValue", 0f),
                PlayerPrefs.GetFloat("blueValue", 0f));
        }

        public static void InitNameState()
        {
            string name = PlayerPrefs.GetString("playerName", "gorilla");
            SetName(name);
        }

        public static void InitTurnState()
        {
            GorillaSnapTurn gorillaTurn = GorillaTagger.Instance.GetComponent<GorillaSnapTurn>();
            string defaultValue = Application.platform == RuntimePlatform.Android ? "NONE" : "SNAP";
            string turnType = PlayerPrefs.GetString("stickTurning", defaultValue);
            int turnValue = PlayerPrefs.GetInt("turnFactor", 4);
            gorillaTurn.ChangeTurnMode(turnType, turnValue);
        }

        public static void InitMicState()
        {
            SetPttMode(GetPttMode());
        }

        public static void InitVoiceMode()
        {
            SetVoiceMode(GetVoiceMode());
        }

        public static void InitItemMode()
        {
            SetItemMode(GetItemMode());
        }

        public static void InitSupportMode()
        {
            displaySupportTab = false;
        }

        public static void InitAll()
        {
            InitColorState();
            InitNameState();
            InitTurnState();
            InitMicState();
            InitVoiceMode();
            InitItemMode();
            InitSupportMode();

            if (CheckForComputer(out GorillaComputer computer))
            {
                computer.InvokeMethod("Start");
            }
        }

        #endregion

        public static bool CheckForComputer(out GorillaComputer computer)
        {
            if (GorillaComputer.instance == null)
            {
                computer = null;
                return false;
            }

            computer = GorillaComputer.instance;
            return true;
        }

        public enum ETurnMode
        {
            Snap,
            Smooth,
            None
        }

        public enum EPTTMode
        {
            AllChat,
            PushToTalk,
            PushToMute
        }
    }
}

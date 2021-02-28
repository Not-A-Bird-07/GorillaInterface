﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ComputerInterface.Interfaces;
using ComputerInterface.ViewLib;
using ComputerInterface.Views;
using TMPro;
using UnityEngine;
using Zenject;

namespace ComputerInterface
{
    public class CustomComputer : MonoBehaviour, IInitializable
    {
        private GorillaComputer _gorillaComputer;
        private ComputerViewController _computerViewController;

        private readonly Dictionary<Type, IComputerView> _cachedViews = new Dictionary<Type, IComputerView>();

        private ComputerViewPlaceholderFactory _viewFactory;

        private bool _initialized;

        private MainMenuView _mainMenuView;

        private CustomScreenInfo _customScreenInfo;

        private List<CustomKeyboardKey> _keys;

        private AssetsLoader _assetsLoader;

        private CIConfig _config;

        void Awake()
        {
            enabled = false;
        }

        [Inject]
        internal async void Construct(
            CIConfig config,
            AssetsLoader assetsLoader,
            MainMenuView mainMenuView,
            ComputerViewPlaceholderFactory viewFactory,
            List<IComputerModEntry> computerModEntries)
        {
            if (_initialized) return;
            _initialized = true;

            Debug.Log($"Found {computerModEntries.Count} computer mod entries");

            _config = config;
            _assetsLoader = assetsLoader;

            _mainMenuView = mainMenuView;
            _cachedViews.Add(typeof(MainMenuView), _mainMenuView);

            _viewFactory = viewFactory;

            _gorillaComputer = GetComponent<GorillaComputer>();
            _gorillaComputer.enabled = false;
            GorillaComputer.instance = _gorillaComputer;

            _computerViewController = new ComputerViewController();
            _computerViewController.OnTextChanged += SetText;
            _computerViewController.OnSwitchView += SwitchView;

            ReplaceKeys();
            _customScreenInfo = await CreateMonitor();
            _customScreenInfo.Color = _config.ScreenBackgroundColor.Value;
            BaseGameInterface.InitAll();

            enabled = true;

            ShowInitialView(_mainMenuView, computerModEntries);

            Debug.Log("Initialized Computer");
        }

        private void ShowInitialView(MainMenuView view, List<IComputerModEntry> computerModEntries)
        {
            _computerViewController.SetView(view, null);
            view.ShowMods(computerModEntries);
        }

        public void Initialize()
        {
        }

        public void Reposition()
        {
            var monitor = transform.Find("monitor");
            monitor.gameObject.SetActive(false);
        }

        public void SetText(string text)
        {
            _customScreenInfo.Text = text;
        }

        public void SetBG(float r, float g, float b)
        {
            _customScreenInfo.Color = new Color(r, g, b);
            _config.ScreenBackgroundColor.Value = _customScreenInfo.Color;
        }

        public void PressButton(CustomKeyboardKey key)
        {
            _computerViewController.NotifyOfKeyPress(key.KeyboardKey);
        }

        private void SwitchView(ComputerViewSwitchEventArgs args)
        {
            if (args.SourceType == args.DestinationType) return;

            var destinationView = GetOrCreateView(args.DestinationType);

            if (destinationView == null)
            {
                return;
            }

            destinationView.CallerViewType = args.SourceType;
            _computerViewController.SetView(destinationView, args.Args);
        }

        private IComputerView GetOrCreateView(Type type)
        {
            if (_cachedViews.TryGetValue(type, out var view))
            {
                return view;
            }

            var newView = _viewFactory.Create(type);
            _cachedViews.Add(type, newView);
            return newView;
        }

        private void ReplaceKeys()
        {
            _keys = new List<CustomKeyboardKey>();

            var nameToEnum = new Dictionary<string, EKeyboardKey>();

            foreach (var enumString in Enum.GetNames(typeof(EKeyboardKey)))
            {
                var key = enumString.Replace("NUM", "").ToLower();
                nameToEnum.Add(key, (EKeyboardKey)Enum.Parse(typeof(EKeyboardKey), enumString));
            }

            foreach(var button in GetComponentsInChildren<GorillaKeyboardButton>())
            {
                if (!nameToEnum.TryGetValue(button.characterString.ToLower(), out var key)) continue;

                var customButton = button.gameObject.AddComponent<CustomKeyboardKey>();
                customButton.pressTime = button.pressTime;
                customButton.functionKey = button.functionKey;
                customButton.sliderValues = button.sliderValues;

                DestroyImmediate(button);

                customButton.Init(this, key);
                _keys.Add(customButton);
            }

            var enterKey = _keys.First(x => x.KeyboardKey == EKeyboardKey.Enter);
            var mKey = _keys.First(x => x.KeyboardKey == EKeyboardKey.M);
            var deleteKey = _keys.First(x => x.KeyboardKey == EKeyboardKey.Delete);

            var spaceKey = Instantiate(enterKey.gameObject, enterKey.transform.parent);
            spaceKey.name = "Space";
            spaceKey.transform.localPosition += new Vector3(2.6f, 0, 3);
            spaceKey.GetComponent<CustomKeyboardKey>().Init(this, EKeyboardKey.Space, "Space");

            var newDeleteKey = Instantiate(deleteKey.gameObject, deleteKey.transform.parent);
            newDeleteKey.name = "Delete";
            newDeleteKey.transform.localPosition += new Vector3(2.3f, 0, 0);
            newDeleteKey.GetComponent<CustomKeyboardKey>().Init(this, EKeyboardKey.Delete);

            deleteKey.GetComponent<CustomKeyboardKey>().Init(this, EKeyboardKey.Back, "Back");

            var leftKey = Instantiate(mKey.gameObject, mKey.transform.parent);
            leftKey.name = "Left";
            leftKey.transform.localPosition += new Vector3(0, 0, 5.6f);
            leftKey.GetComponent<CustomKeyboardKey>().Init(this, EKeyboardKey.Left, ".");

            var downKey = Instantiate(leftKey.gameObject, leftKey.transform.parent);
            downKey.name = "Down";
            downKey.transform.localPosition += new Vector3(0, 0, 2.3f);
            downKey.GetComponent<CustomKeyboardKey>().Init(this, EKeyboardKey.Down, ".");

            var rightKey = Instantiate(downKey.gameObject, downKey.transform.parent);
            rightKey.name = "Right";
            rightKey.transform.localPosition += new Vector3(0, 0, 2.3f);
            rightKey.GetComponent<CustomKeyboardKey>().Init(this, EKeyboardKey.Right, ".");

            var upKey = Instantiate(downKey.gameObject, downKey.transform.parent);
            upKey.name = "Up";
            upKey.transform.localPosition += new Vector3(-2.3f, 0, 0);
            upKey.GetComponent<CustomKeyboardKey>().Init(this, EKeyboardKey.Up, ".");
        }

        private async Task<CustomScreenInfo> CreateMonitor()
        {
            transform.Find("monitor").gameObject.SetActive(false);

            var tmpSettings = await _assetsLoader.GetAsset<TMP_Settings>("TMP Settings");
            typeof(TMP_Settings).GetField(
                    "s_Instance",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)?
                .SetValue(null, tmpSettings);

            var monitorAsset = await _assetsLoader.GetAsset<GameObject>("monitor");


            var newMonitor = Instantiate(monitorAsset);
            newMonitor.name = "Custom Monitor";
            newMonitor.transform.localScale = new Vector3(0.4f, 0.4f, 0.4f);
            newMonitor.transform.eulerAngles = new Vector3(0, 90, 0);
            newMonitor.transform.position = new Vector3(-69f, 12.02f, -82.8f);

            var info = new CustomScreenInfo();

            info.Transform = newMonitor.transform;
            info.TextMeshProUgui = newMonitor.GetComponentInChildren<TextMeshProUGUI>();
            info.Renderer = newMonitor.GetComponentInChildren<MeshRenderer>();
            info.Materials = info.Renderer.materials;

            info.Color = new Color(0.05f, 0.05f, 0.05f);
            info.FontSize = 80f;

            return info;
        }
    }
}
﻿using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using YARG.Core;
using YARG.Core.Logging;
using YARG.Gameplay.Visuals;

namespace YARG.Themes
{
    public class ThemeManager : MonoSingleton<ThemeManager>
    {
        private readonly Dictionary<ThemePreset, ThemeContainer> _themeContainers = new();

        private void Start()
        {
            // Populate all of the default themes
            foreach (var defaultPreset in ThemePreset.Defaults)
            {
                _themeContainers.Add(defaultPreset, defaultPreset.CreateThemeContainer());
            }
        }

        public GameObject CreateNotePrefabFromTheme(ThemePreset preset, GameMode gameMode, GameObject noModelPrefab)
        {
            // Get the theme container
            var container = GetThemeContainer(preset, gameMode);
            if (container is null)
            {
                return null;
            }

            var prefabKey = (gameMode, typeof(IThemeNoteCreator), 0);

            // Try to get and return a cached version, otherwise we'll have to create it
            var cached = container.PrefabCache.GetValueOrDefault(prefabKey);
            if (cached != null)
            {
                return cached;
            }

            // Duplicate the prefab
            var gameObject = Instantiate(noModelPrefab, transform);
            var prefabCreator = gameObject.GetComponent<IThemeNoteCreator>();

            // Set the models
            var themeComp = container.GetThemeComponent();
            prefabCreator.SetThemeModels(
                themeComp.GetNoteModelsForGameMode(gameMode, false),
                themeComp.GetNoteModelsForGameMode(gameMode, true));

            // Disable and return
            gameObject.SetActive(false);
            container.PrefabCache[prefabKey] = gameObject;
            return gameObject;
        }

        public GameObject CreateFretPrefabFromTheme(ThemePreset preset, GameMode gameMode, int variant = 0)
        {
            return CreatePrefabFromTheme<ThemeFret, Fret>(preset, gameMode, variant);
        }

        public GameObject CreateKickFretPrefabFromTheme(ThemePreset preset, GameMode gameMode)
        {
            return CreatePrefabFromTheme<ThemeKickFret, KickFret>(preset, gameMode, 0);
        }

        private GameObject CreatePrefabFromTheme<TTheme, TBind>(ThemePreset preset, GameMode gameMode, int variant)
            where TBind : MonoBehaviour, IThemeBindable<TTheme>
        {
            // Get the theme container
            var container = GetThemeContainer(preset, gameMode);
            if (container is null)
            {
                return null;
            }

            var prefabKey = (gameMode, typeof(TTheme), variant);

            // Try to get and return a cached version, otherwise we'll have to create it
            var cached = container.PrefabCache.GetValueOrDefault(prefabKey);
            if (cached != null)
            {
                return cached;
            }

            // Duplicate the prefab
            var prefab = container.GetThemeComponent().GetModelForGameMode<TTheme>(gameMode);
            var gameObject = Instantiate(prefab, transform);

            // Set info
            var bindComp = gameObject.AddComponent<TBind>();
            bindComp.ThemeBind = gameObject.GetComponent<TTheme>();

            // Disable and return
            gameObject.SetActive(false);
            container.PrefabCache[prefabKey] = gameObject;
            return gameObject;
        }

        public ThemeContainer GetThemeContainer(ThemePreset preset, GameMode mode)
        {
            // Check if the theme supports the game mode
            if (!preset.SupportedGameModes.Contains(mode))
            {
                YargLogger.LogFormatInfo("Theme `{0}` does not support `{1}`. Falling back to the default theme.",
                    preset.Name, mode);
                preset = ThemePreset.Default;
            }

            // Get the theme container
            var container = _themeContainers.GetValueOrDefault(preset);
            if (container is null)
            {
                YargLogger.LogFormatWarning("Could not find theme with ID `{0}`!", preset.Id);
                return null;
            }

            return container;
        }
    }
}
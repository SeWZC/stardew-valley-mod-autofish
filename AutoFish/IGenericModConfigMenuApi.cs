﻿using System;
using StardewModdingAPI;

namespace AutoFish
{
    /// <summary>The API which lets other mods add a config UI through Generic Mod Config Menu.</summary>
    public interface IGenericModConfigMenuApi
    {
        /*********
        ** Methods
        *********/
        /****
        ** Must be called first
        ****/
        /// <summary>Register a mod whose config can be edited through the UI.</summary>
        /// <param name="mod">The mod's manifest.</param>
        /// <param name="reset">Reset the mod's config to its default values.</param>
        /// <param name="save">Save the mod's current config to the <c>config.json</c> file.</param>
        /// <param name="titleScreenOnly">Whether the options can only be edited from the title screen.</param>
        /// <remarks>
        ///     Each mod can only be registered once, unless it's deleted via <see cref="Unregister" /> before calling this
        ///     again.
        /// </remarks>
        void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);

        /// <summary>Add a boolean option at the current position in the form.</summary>
        /// <param name="mod">The mod's manifest.</param>
        /// <param name="getValue">Get the current value from the mod config.</param>
        /// <param name="setValue">Set a new value in the mod config.</param>
        /// <param name="name">The label text to show in the form.</param>
        /// <param name="tooltip">
        ///     The tooltip text shown when the cursor hovers on the field, or <c>null</c> to disable the
        ///     tooltip.
        /// </param>
        /// <param name="fieldId">
        ///     The unique field ID for use with <see cref="OnFieldChanged" />, or <c>null</c> to auto-generate a
        ///     randomized ID.
        /// </param>
        void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string>? tooltip = null, string? fieldId = null);

        /// <summary>Register a method to notify when any option registered by this mod is edited through the config UI.</summary>
        /// <param name="mod">The mod's manifest.</param>
        /// <param name="onChange">The method to call with the option's unique field ID and new value.</param>
        /// <remarks>
        ///     Options use a randomized ID by default; you'll likely want to specify the <c>fieldId</c> argument when adding
        ///     options if you use this.
        /// </remarks>
        void OnFieldChanged(IManifest mod, Action<string, object> onChange);

        /// <summary>Remove a mod from the config UI and delete all its options and pages.</summary>
        /// <param name="mod">The mod's manifest.</param>
        void Unregister(IManifest mod);
    }
}
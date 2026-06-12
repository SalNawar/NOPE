// ReSharper disable InconsistentNaming
using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Channel an effect broadcasts on. Receivers (visuals, music, UI, visitor pool,
/// shop, newsletter, tech tree, chatter...) query the active-effect registry for
/// their channel and react. New systems = new channel or new receiver, no core changes.
/// </summary>
public enum EffectChannel
{
 
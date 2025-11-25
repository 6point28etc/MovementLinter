/*
 * MIT License
 *
 * Copyright (c) 2020 DemoJameson
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using Microsoft.Xna.Framework;
using Monocle;

namespace Celeste.Mod.MovementLinter;

public class Tooltip {
    // SRT automatically finds and saves static fields of all Entities, so hide these away in a non-Entity wrapper class
    private static readonly Lazy<Assembly> speedrunToolAssembly = new(() =>
        AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(
            (Assembly a) => a.GetName().Name == "SpeedrunTool"));
    // 0 bits indicate slots that are currently filled, 1 bits indicate empty slots.
    public static uint freeHeightsMask = 0xFFFF_FFFF;

    private class TooltipEntity : Entity {
        private const int Padding = 25;
        private readonly string message;
        private readonly float shownDurationSeconds;
        private readonly int heightIndex;
        private float alpha;
        private float unEasedAlpha;
        private bool freedSlot = false;

        public TooltipEntity(string message, float shownDurationSeconds, int heightIndex) {
            freeHeightsMask &= (uint) ~(1 << heightIndex);
            this.message              = message;
            this.shownDurationSeconds = shownDurationSeconds;
            this.heightIndex          = heightIndex;
            Position                  = new(Padding,
                                            Engine.Height - (heightIndex + 1) * (ActiveFont.LineHeight + Padding / 2f));
            Tag = Tags.HUD | Tags.Global | Tags.FrozenUpdate | Tags.PauseUpdate| Tags.TransitionUpdate;
            Add(new Coroutine(Show()));
            if (speedrunToolAssembly.Value != null) {
                IgnoreSaveLoad();
            }
        }

        ~TooltipEntity() {
            if (!freedSlot) {
                freeHeightsMask |= (uint) 1 << heightIndex;
            }
        }

        private void IgnoreSaveLoad() {
            // Speedrun tool very annoyingly moved this frome SaveLoad.IgnoreSaveLoadComponent to
            // SaveLoad.Utils.IgnoreSaveLoadComponent post-Core, so we have to look for both
            try {
                Add((Component) speedrunToolAssembly.Value
                    .GetType("Celeste.Mod.SpeedrunTool.SaveLoad.IgnoreSaveLoadComponent", true)
                    .GetConstructor([]).Invoke(null));
            } catch (TypeLoadException) {
                try {
                    Add((Component) speedrunToolAssembly.Value
                        .GetType("Celeste.Mod.SpeedrunTool.SaveLoad.Utils.IgnoreSaveLoadComponent", true)
                        .GetConstructor([]).Invoke(null));
                } catch (TypeLoadException) {
                    // If we couldn't find either, just accept we can't ignore save / load and continue, it'll only
                    // be a minor visual bug
                }
            }
        }

        private IEnumerator Show() {
            while (alpha < 1f) {
                unEasedAlpha = Calc.Approach(unEasedAlpha, 1f, Engine.RawDeltaTime * 5f);
                alpha = Ease.SineOut(unEasedAlpha);
                yield return null;
            }

            yield return Dismiss();
        }

        private IEnumerator Dismiss() {
            yield return shownDurationSeconds;
            while (alpha > 0f) {
                unEasedAlpha = Calc.Approach(unEasedAlpha, 0f, Engine.RawDeltaTime * 5f);
                alpha        = Ease.SineIn(unEasedAlpha);
                yield return null;
            }

            freeHeightsMask |= (uint) 1 << heightIndex;
            freedSlot = true;
            RemoveSelf();
        }

        public override void Render() {
            base.Render();
            ActiveFont.DrawOutline(message, Position, Vector2.Zero, Vector2.One, Color.White * alpha, 2,
                Color.Black * alpha * alpha * alpha);
        }
    }

    public static void Show(string message, float shownDurationSeconds = 2f) {
        // If there are already 32 tooltips on screen, there's no room to show this one anyway, so just don't
        if (Engine.Scene is {} scene && freeHeightsMask != 0) {
            // Software-implement ffs since c# sucks. There are bithacking ways to do this
            // but they're cursed and we should be dealing with small numbers so whatever
            int heightIndex = 0;
            while ((freeHeightsMask & (1 << heightIndex)) == 0) {
                ++heightIndex;
            }
            scene.Add(new TooltipEntity(message, shownDurationSeconds, heightIndex));
        }
    }
}

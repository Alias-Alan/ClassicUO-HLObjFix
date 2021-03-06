﻿#region license

// Copyright (C) 2020 ClassicUO Development Community on Github
// 
// This project is an alternative client for the game Ultima Online.
// The goal of this is to develop a lightweight client considering
// new technologies.
// 
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
// 
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <https://www.gnu.org/licenses/>.

#endregion

using ClassicUO.Configuration;
using ClassicUO.Game.Data;
using ClassicUO.Game.GameObjects;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using Microsoft.Xna.Framework;

namespace ClassicUO.Game.Managers
{
    internal class HealthLinesManager
    {
        private const int BAR_WIDTH = 34; //28;
        private const int BAR_HEIGHT = 8;
        private const int BAR_WIDTH_HALF = BAR_WIDTH >> 1;
        private const int BAR_HEIGHT_HALF = BAR_HEIGHT >> 1;


        private readonly UOTexture32 _background_texture, _hp_texture;
        private Vector3 _vectorHue = Vector3.Zero;

        public HealthLinesManager()
        {
            _background_texture = GumpsLoader.Instance.GetTexture(0x1068);
            _hp_texture = GumpsLoader.Instance.GetTexture(0x1069);
        }

        public bool IsEnabled => ProfileManager.Current != null && ProfileManager.Current.ShowMobilesHP;


        public void Update()
        {
            if (_background_texture != null)
            {
                _background_texture.Ticks = Time.Ticks;
            }

            if (_hp_texture != null)
            {
                _hp_texture.Ticks = Time.Ticks;
            }
        }

        public void Draw(UltimaBatcher2D batcher)
        {
            int screenW = ProfileManager.Current.GameWindowSize.X;
            int screenH = ProfileManager.Current.GameWindowSize.Y;

            if (SerialHelper.IsMobile(TargetManager.LastTargetInfo.Serial))
            {
                DrawHealthLineWithMath(batcher, TargetManager.LastTargetInfo.Serial, screenW, screenH);
            }

            if (SerialHelper.IsMobile(TargetManager.SelectedTarget))
            {
                DrawHealthLineWithMath(batcher, TargetManager.SelectedTarget, screenW, screenH);
            }

            if (SerialHelper.IsMobile(TargetManager.LastAttack))
            {
                DrawHealthLineWithMath(batcher, TargetManager.LastAttack, screenW, screenH);
            }

            if (!IsEnabled)
            {
                return;
            }

            int mode = ProfileManager.Current.MobileHPType;

            if (mode < 0)
            {
                return;
            }

            int showWhen = ProfileManager.Current.MobileHPShowWhen;

            foreach (Mobile mobile in World.Mobiles)
            {
                if (mobile.IsDestroyed)
                {
                    continue;
                }

                int current = mobile.Hits;
                int max = mobile.HitsMax;

                if (showWhen == 1 && current == max)
                {
                    continue;
                }

                Point p = mobile.RealScreenPosition;
                p.X += (int) mobile.Offset.X + 22 + 5;
                p.Y += (int) (mobile.Offset.Y - mobile.Offset.Z) + 22 + 5;


                if (mode != 1 && !mobile.IsDead)
                {
                    if (showWhen == 2 && current != max || showWhen <= 1)
                    {
                        if (mobile.HitsPercentage != 0)
                        {
                            AnimationsLoader.Instance.GetAnimationDimensions
                            (
                                mobile.AnimIndex,
                                mobile.GetGraphicForAnimation(),
                                /*(byte) m.GetDirectionForAnimation()*/ 0,
                                /*Mobile.GetGroupForAnimation(m, isParent:true)*/ 0,
                                mobile.IsMounted,
                                /*(byte) m.AnimIndex*/ 0,
                                out int centerX,
                                out int centerY,
                                out int width,
                                out int height
                            );

                            Point p1 = p;
                            p1.Y -= height + centerY + 8 + 22;

                            if (mobile.IsGargoyle && mobile.IsFlying)
                            {
                                p1.Y -= 22;
                            }
                            else if (!mobile.IsMounted)
                            {
                                p1.Y += 22;
                            }

                            p1 = Client.Game.Scene.Camera.WorldToScreen(p1);
                            p1.X -= (mobile.HitsTexture.Width >> 1) + 5;
                            p1.Y -= mobile.HitsTexture.Height;

                            if (mobile.ObjectHandlesOpened)
                            {
                                p1.Y -= Constants.OBJECT_HANDLES_GUMP_HEIGHT + 5;
                            }

                            if (!(p1.X < 0 || p1.X > screenW - mobile.HitsTexture.Width || p1.Y < 0 || p1.Y > screenH))
                            {
                                mobile.HitsTexture.Draw(batcher, p1.X, p1.Y);
                            }
                        }
                    }
                }

                if (mobile.Serial == TargetManager.LastTargetInfo.Serial ||
                    mobile.Serial == TargetManager.SelectedTarget ||
                    mobile.Serial == TargetManager.LastAttack)
                {
                    continue;
                }

                p.X -= 5;
                p = Client.Game.Scene.Camera.WorldToScreen(p);
                p.X -= BAR_WIDTH_HALF;
                p.Y -= BAR_HEIGHT_HALF;

                if (p.X < 0 || p.X > screenW - BAR_WIDTH)
                {
                    continue;
                }

                if (p.Y < 0 || p.Y > screenH - BAR_HEIGHT)
                {
                    continue;
                }

                if (mode >= 1)
                {
                    DrawHealthLine(batcher, mobile, p.X, p.Y, mobile.Serial != World.Player.Serial);
                }
            }
        }

        private void DrawHealthLineWithMath(UltimaBatcher2D batcher, uint serial, int screenW, int screenH)
        {
            Entity entity = World.Get(serial);

            if (entity == null)
            {
                return;
            }

            Point p = entity.RealScreenPosition;
            p.X += (int) entity.Offset.X + 22;
            p.Y += (int) (entity.Offset.Y - entity.Offset.Z) + 22 + 5;

            p = Client.Game.Scene.Camera.WorldToScreen(p);
            p.X -= BAR_WIDTH_HALF;
            p.Y -= BAR_HEIGHT_HALF;

            if (p.X < 0 || p.X > screenW - BAR_WIDTH)
            {
                return;
            }

            if (p.Y < 0 || p.Y > screenH - BAR_HEIGHT)
            {
                return;
            }

            DrawHealthLine(batcher, entity, p.X, p.Y, false);
        }

        private void DrawHealthLine(UltimaBatcher2D batcher, Entity entity, int x, int y, bool passive)
        {
            if (entity == null)
            {
                return;
            }

            int per = BAR_WIDTH * entity.HitsPercentage / 100;

            Mobile mobile = entity as Mobile;


            float alpha = passive ? 0.5f : 0.0f;

            _vectorHue.X = mobile != null ? Notoriety.GetHue(mobile.NotorietyFlag) : Notoriety.GetHue(NotorietyFlag.Gray);
            _vectorHue.Y = 1;
            _vectorHue.Z = alpha;

            if (mobile == null)
            {
                y += 22;
            }


            const int MULTIPLER = 1;

            batcher.Draw2D
            (
                _background_texture,
                x, y,
                _background_texture.Width * MULTIPLER,
                _background_texture.Height * MULTIPLER,
                ref _vectorHue
            );


            _vectorHue.X = 0x21;


            if (entity.Hits != entity.HitsMax || entity.HitsMax == 0)
            {
                int offset = 2;

                if (per >> 2 == 0)
                {
                    offset = per;
                }

                batcher.Draw2DTiled
                (
                    _hp_texture,
                    x + per * MULTIPLER - offset, y,
                    (BAR_WIDTH - per) * MULTIPLER - offset / 2,
                    _hp_texture.Height * MULTIPLER,
                    ref _vectorHue
                );
            }

            ushort hue = 90;

            if (per > 0)
            {
                if (mobile != null)
                {
                    if (mobile.IsPoisoned)
                    {
                        hue = 63;
                    }
                    else if (mobile.IsYellowHits)
                    {
                        hue = 53;
                    }
                }

                _vectorHue.X = hue;


                batcher.Draw2DTiled
                (
                    _hp_texture,
                    x, y,
                    per * MULTIPLER,
                    _hp_texture.Height * MULTIPLER,
                    ref _vectorHue
                );
            }
        }
    }
}
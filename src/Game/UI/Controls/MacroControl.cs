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

using System;
using System.Collections.Generic;
using System.Linq;
using ClassicUO.Game.Managers;
using ClassicUO.Game.Scenes;
using ClassicUO.Game.UI.Gumps;
using ClassicUO.Input;
using ClassicUO.IO.Resources;
using ClassicUO.Renderer;
using ClassicUO.Resources;
using SDL2;

namespace ClassicUO.Game.UI.Controls
{
    internal class MacroControl : Control
    {
        private readonly MacroCollectionControl _collection;
        private readonly HotkeyBox _hotkeyBox;

        public MacroControl(string name)
        {
            CanMove = true;

            _hotkeyBox = new HotkeyBox();

            _hotkeyBox.HotkeyChanged += BoxOnHotkeyChanged;
            _hotkeyBox.HotkeyCancelled += BoxOnHotkeyCancelled;


            Add(_hotkeyBox);

            Add(new NiceButton(0, _hotkeyBox.Height + 3, 170, 25, ButtonAction.Activate, ResGumps.CreateMacroButton, 0, TEXT_ALIGN_TYPE.TS_LEFT) {ButtonParameter = 2, IsSelectable = false});

            Add(new NiceButton(0, _hotkeyBox.Height + 30, 50, 25, ButtonAction.Activate, ResGumps.Add) {IsSelectable = false});
            Add(new NiceButton(52, _hotkeyBox.Height + 30, 50, 25, ButtonAction.Activate, ResGumps.Remove) {ButtonParameter = 1, IsSelectable = false});


            Add
            (
                _collection = new MacroCollectionControl(name, 280, 280)
                {
                    Y = _hotkeyBox.Height + 50 + 10
                }
            );

            SetupKeyByDefault();
        }


        private void SetupKeyByDefault()
        {
            if (_collection?.Macro == null || _hotkeyBox == null)
            {
                return;
            }

            if (_collection.Macro.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                SDL.SDL_Keymod mod = SDL.SDL_Keymod.KMOD_NONE;

                if (_collection.Macro.Alt)
                {
                    mod |= SDL.SDL_Keymod.KMOD_ALT;
                }

                if (_collection.Macro.Shift)
                {
                    mod |= SDL.SDL_Keymod.KMOD_SHIFT;
                }

                if (_collection.Macro.Ctrl)
                {
                    mod |= SDL.SDL_Keymod.KMOD_CTRL;
                }

                _hotkeyBox.SetKey(_collection.Macro.Key, mod);
            }
        }

        private void BoxOnHotkeyChanged(object sender, EventArgs e)
        {
            bool shift = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_SHIFT) != SDL.SDL_Keymod.KMOD_NONE;
            bool alt = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_ALT) != SDL.SDL_Keymod.KMOD_NONE;
            bool ctrl = (_hotkeyBox.Mod & SDL.SDL_Keymod.KMOD_CTRL) != SDL.SDL_Keymod.KMOD_NONE;

            if (_hotkeyBox.Key != SDL.SDL_Keycode.SDLK_UNKNOWN)
            {
                Macro macro = Client.Game.GetScene<GameScene>()
                                    .Macros.FindMacro(_hotkeyBox.Key, alt, ctrl, shift);

                if (macro != null)
                {
                    if (_collection.Macro == macro)
                    {
                        return;
                    }

                    SetupKeyByDefault();
                    UIManager.Add(new MessageBoxGump(250, 150, ResGumps.ThisKeyCombinationAlreadyExists, null));

                    return;
                }
            }
            else
            {
                return;
            }

            Macro m = _collection.Macro;
            m.Key = _hotkeyBox.Key;
            m.Shift = shift;
            m.Alt = alt;
            m.Ctrl = ctrl;
        }

        private void BoxOnHotkeyCancelled(object sender, EventArgs e)
        {
            Macro m = _collection.Macro;
            m.Alt = m.Ctrl = m.Shift = false;
            m.Key = SDL.SDL_Keycode.SDLK_UNKNOWN;
        }

        public override void OnButtonClick(int buttonID)
        {
            if (buttonID == 0) // add
            {
                _collection.AddEmpty();
            }
            else if (buttonID == 1) // remove
            {
                _collection.RemoveLast();
            }
            else if (buttonID == 2) // add macro button
            {
                UIManager.Gumps.OfType<MacroButtonGump>()
                         .FirstOrDefault(s => s._macro == _collection.Macro)
                         ?.Dispose();

                MacroButtonGump macroButtonGump = new MacroButtonGump(_collection.Macro, Mouse.Position.X, Mouse.Position.Y);
                UIManager.Add(macroButtonGump);
            }
        }
    }


    internal class MacroCollectionControl : Control
    {
        private readonly List<Combobox> _comboboxes = new List<Combobox>();
        private readonly ScrollArea _scrollArea;

        public MacroCollectionControl(string name, int w, int h)
        {
            CanMove = true;
            _scrollArea = new ScrollArea(0, 50, w, h, true);
            Add(_scrollArea);


            GameScene scene = Client.Game.GetScene<GameScene>();

            foreach (Macro macro in scene.Macros.GetAllMacros())
            {
                if (macro.Name == name)
                {
                    Macro = macro;

                    break;
                }
            }

            if (Macro == null)
            {
                Macro = Macro.CreateEmptyMacro(name);
                scene.Macros.AppendMacro(Macro);

                CreateCombobox(Macro.FirstNode);
            }
            else
            {
                MacroObject o = Macro.FirstNode;

                while (o != null)
                {
                    CreateCombobox(o);
                    o = o.Right;
                }
            }
        }

        public Macro Macro { get; }


        public void AddEmpty()
        {
            MacroObject ob = Macro.FirstNode;

            if (ob.Code == MacroType.None)
            {
                return;
            }

            while (ob.Right != null)
            {
                if (ob.Right.Code == MacroType.None)
                {
                    return;
                }

                ob = ob.Right;
            }

            MacroObject obj = Macro.Create(MacroType.None);

            ob.Right = obj;
            obj.Left = ob;
            obj.Right = null;

            CreateCombobox(obj);
        }

        private void CreateCombobox(MacroObject obj)
        {
            Combobox box = new Combobox(0, 0, 200, Enum.GetNames(typeof(MacroType)), (int) obj.Code, 300);

            box.OnOptionSelected += (sender, e) =>
            {
                Combobox b = (Combobox) sender;

                if (b.SelectedIndex == 0) // MacroType.None
                {
                    MacroObject m = (MacroObject) b.Tag;

                    if (Macro.FirstNode == m)
                    {
                        Macro.FirstNode = m.Right;
                    }

                    if (m.Right != null)
                    {
                        m.Right.Left = m.Left;
                    }

                    if (m.Left != null)
                    {
                        m.Left.Right = m.Right;
                    }

                    m.Left = null;
                    m.Right = null;

                    b.Tag = null;

                    b.Parent.Dispose();
                    _comboboxes.Remove(b);

                    if (_comboboxes.Count == 0 || _comboboxes.All(s => s.IsDisposed))
                    {
                        Macro.FirstNode = Macro.Create(MacroType.None);
                        CreateCombobox(Macro.FirstNode);
                    }
                }
                else
                {
                    MacroType t = (MacroType) b.SelectedIndex;

                    MacroObject m = (MacroObject) b.Tag;
                    MacroObject newmacro = Macro.Create(t);

                    MacroObject left = m.Left;
                    MacroObject right = m.Right;

                    if (left != null)
                    {
                        left.Right = newmacro;
                    }

                    if (right != null)
                    {
                        right.Left = newmacro;
                    }

                    newmacro.Left = left;
                    newmacro.Right = right;

                    b.Tag = newmacro;

                    if (Macro.FirstNode == m)
                    {
                        Macro.FirstNode = newmacro;
                    }


                    b.Parent.Children
                     .Where(s => s != b)
                     .ToList()
                     .ForEach(s => s.Dispose());

                    switch (newmacro.SubMenuType)
                    {
                        case 1: // another combo
                            int count = 0;
                            int offset = 0;

                            Macro.GetBoundByCode(t, ref count, ref offset);

                            Combobox subBox = new Combobox
                            (
                                20, b.Height + 2, 180, Enum.GetNames(typeof(MacroSubType))
                                                           .Skip(offset)
                                                           .Take(count)
                                                           .ToArray(), 0, 300
                            );


                            subBox.OnOptionSelected += (ss, ee) =>
                            {
                                Macro.GetBoundByCode(newmacro.Code, ref count, ref offset);
                                MacroSubType subType = (MacroSubType) (offset + ee);
                                newmacro.SubCode = subType;
                            };

                            b.Parent.Add(subBox);
                            b.Parent.WantUpdateSize = true;

                            break;

                        case 2: // string

                            b.Parent.Add
                            (
                                new ResizePic(0x0BB8)
                                {
                                    X = 18,
                                    Y = b.Height + 2,
                                    Width = 240,
                                    Height = b.Height * 2 + 4
                                }
                            );

                            StbTextBox textbox = new StbTextBox(0xFF, 80, 236, true, FontStyle.BlackBorder)
                            {
                                X = 20,
                                Y = b.Height + 5,
                                Width = 236,
                                Height = b.Height * 2
                            };

                            textbox.TextChanged += (sss, eee) =>
                            {
                                if (newmacro.HasString())
                                {
                                    ((MacroObjectString) newmacro).Text = ((StbTextBox) sss).Text;
                                }
                            };

                            b.Parent.Add(textbox);
                            b.Parent.WantUpdateSize = true;

                            break;
                    }
                }
            };

            box.Tag = obj;
            _scrollArea.Add(box);
            _comboboxes.Add(box);


            if (obj.Code != MacroType.None)
            {
                switch (obj.SubMenuType)
                {
                    case 1:
                        int count = 0;
                        int offset = 0;

                        Macro.GetBoundByCode(obj.Code, ref count, ref offset);

                        Combobox subBox = new Combobox
                        (
                            20, box.Height + 2, 180, Enum.GetNames(typeof(MacroSubType))
                                                         .Skip(offset)
                                                         .Take(count)
                                                         .ToArray(), (int) (obj.SubCode - offset), 300
                        );


                        subBox.OnOptionSelected += (ss, ee) =>
                        {
                            Macro.GetBoundByCode(obj.Code, ref count, ref offset);
                            MacroSubType subType = (MacroSubType) (offset + ee);
                            obj.SubCode = subType;
                        };

                        box.Parent.Add(subBox);
                        box.Parent.WantUpdateSize = true;

                        break;

                    case 2:

                        box.Parent.Add
                        (
                            new ResizePic(0x0BB8)
                            {
                                X = 18,
                                Y = box.Height + 2,
                                Width = 240,
                                Height = box.Height * 2 + 4
                            }
                        );

                        StbTextBox textbox = new StbTextBox(0xFF, 80, 236, true, FontStyle.BlackBorder)
                        {
                            X = 20,
                            Y = box.Height + 5,
                            Width = 236,
                            Height = box.Height * 2
                        };

                        textbox.SetText(obj.HasString() ? ((MacroObjectString) obj).Text : string.Empty);

                        textbox.TextChanged += (sss, eee) =>
                        {
                            if (obj.HasString())
                            {
                                ((MacroObjectString) obj).Text = ((StbTextBox) sss).Text;
                            }
                        };

                        box.Parent.Add(textbox);
                        box.Parent.WantUpdateSize = true;

                        break;
                }
            }
        }


        public void RemoveLast()
        {
            //_scrollArea.Children.LastOrDefault()?.Dispose();
            //_comboboxes.RemoveAt(_comboboxes.Count - 1);

            //if (_comboboxes.Count == 0)
            //    AddEmpty();
        }

        public override void Dispose()
        {
            base.Dispose();
            _comboboxes.ForEach(s => s.Parent?.Dispose());
            _comboboxes.Clear();
        }
    }
}
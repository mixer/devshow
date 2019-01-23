﻿/*
 * Mixer Unity SDK
 *
 * Copyright (c) Microsoft Corporation
 * All rights reserved.
 *
 * MIT License
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy of this
 * software and associated documentation files (the "Software"), to deal in the Software
 * without restriction, including without limitation the rights to use, copy, modify, merge,
 * publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
 * to whom the Software is furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all copies or
 * substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED *AS IS*, WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
 * INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
 * PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
 * FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
 * OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
 * DEALINGS IN THE SOFTWARE.
 */
using System;
using System.Collections.Generic;

namespace Microsoft.Mixer
{
    /// <summary>
    /// Represents an interactive label control. All controls are created and 
    /// configured using Interactive Studio.
    /// </summary>
#if !WINDOWS_UWP
    [System.Serializable]
#endif
    public class InteractiveLabelControl : InteractiveControl
    {
        /// <summary>
        /// Text displayed on this control.
        /// </summary>
        public string Text
        {
            get;
            private set;
        }

        /// <summary>
        /// Function to update the text for the label control.
        /// </summary>
        /// <param name="text">String to display on the label.</param>
        public void SetText(string text)
        {
            InteractivityManager interactivityManager = InteractivityManager.SingletonInstance;
            interactivityManager._QueuePropertyUpdate(
                _sceneID, 
                ControlID,
                interactivityManager._InteractiveControlPropertyToString(InteractiveControlProperty.Text), 
                text);
        }

        public InteractiveLabelControl(string controlID, string text, string sceneID) : base(controlID, InteractivityManager._CONTROL_KIND_LABEL, InteractiveEventType.Unknown, false, "", "", sceneID, new Dictionary<string, object>())
        {
            Text = text;
        }
    }
}

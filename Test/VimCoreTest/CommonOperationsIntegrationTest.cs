﻿using Vim.EditorHost;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xunit;

namespace Vim.UnitTest
{
    public abstract class CommonOperationsIntegrationTest : VimTestBase
    {
        private IVimBuffer _vimBuffer;
        private IWpfTextView _textView;
        private ITextBuffer _textBuffer;
        private IVimGlobalSettings _globalSettings;
        private CommonOperations _commonOperationsRaw;
        private ICommonOperations _commonOperations;

        private void Create(params string[] lines)
        {
            _globalSettings = Vim.GlobalSettings;
            _vimBuffer = CreateVimBuffer(lines);
            _textView = (IWpfTextView)_vimBuffer.TextView;
            _textBuffer = _vimBuffer.TextBuffer;
            _commonOperationsRaw = (CommonOperations)CommonOperationsFactory.GetCommonOperations(_vimBuffer.VimBufferData);
            _commonOperations = _commonOperationsRaw;
        }

        public abstract class ScrollOffsetTest : CommonOperationsIntegrationTest
        {
            private static readonly string[] s_lines = KeyInputUtilTest.CharLettersLower.Select(x => x.ToString()).ToArray();
            private readonly IFoldManager _foldManager;
            private readonly int _lastLineNumber = 0;

            protected ScrollOffsetTest()
            {
                Create(s_lines);
                _lastLineNumber = _textBuffer.CurrentSnapshot.LineCount - 1;
                _textView.SetVisibleLineCount(5);
                _foldManager = FoldManagerFactory.GetFoldManager(_textView);
            }

            private void AssertFirstLine(int lineNumber)
            {
                TestableSynchronizationContext.RunAll();
                var actual = _textView.GetFirstVisibleLineNumber();
                Assert.Equal(lineNumber, actual);
            }

            private void AssertLastLine(int lineNumber)
            {
                TestableSynchronizationContext.RunAll();
                var actual = _textView.GetLastVisibleLineNumber();
                Assert.Equal(lineNumber, actual);
            }

            public sealed class TopTest : ScrollOffsetTest
            {
                [WpfFact]
                public void Disabled()
                {
                    _globalSettings.ScrollOffset = 0;
                    _textView.MoveCaretToLine(1);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(0);
                }

                [WpfFact]
                public void Simple()
                {
                    _globalSettings.ScrollOffset = 1;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToBottom();
                    _textView.MoveCaretToLine(2);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(1);
                }

                /// <summary>
                /// Handle the case where the scroll would be to the top of the screen
                /// </summary>
                [WpfFact]
                public void ScrollToTop()
                {
                    _globalSettings.ScrollOffset = 2;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToBottom();
                    _textView.MoveCaretToLine(2);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(0);
                }

                /// <summary>
                /// Handle the case where the correct scroll offset would be above the first line
                /// in the view.  Should just stop at the first line 
                /// </summary>
                [WpfFact]
                public void ScrollAboveTop()
                {
                    _globalSettings.ScrollOffset = 3;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToBottom();
                    _textView.MoveCaretToLine(1);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(0);
                }

                /// <summary>
                /// Handle the case where the 'scrolloff' value is larger than half the lines in the 
                /// visible screen.  At that point the scroll should just center the caret 
                /// </summary>
                [WpfFact]
                public void OffsetTooBig()
                {
                    _globalSettings.ScrollOffset = 100;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToBottom();
                    _textView.MoveCaretToLine(7);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(5);
                }

                /// <summary>
                /// Folded text should count as a single line 
                /// </summary>
                [WpfFact]
                public void OverFold()
                {
                    _globalSettings.ScrollOffset = 2;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToBottom();
                    _foldManager.CreateFold(_textBuffer.GetLineRange(startLine: 3, endLine: 5));
                    _textView.MoveCaretToLine(6);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(2);
                }
            }

            public sealed class BottomTest : ScrollOffsetTest
            {
                [WpfFact]
                public void Disabled()
                {
                    _globalSettings.ScrollOffset = 0;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToTop();
                    _textView.MoveCaretToLine(_lastLineNumber);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertLastLine(4);
                }

                [WpfFact]
                public void Simple()
                {
                    _globalSettings.ScrollOffset = 1;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToTop();
                    _textView.MoveCaretToLine(4);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertLastLine(5);
                }

                [WpfFact]
                public void ScrollToBottom()
                {
                    _globalSettings.ScrollOffset = 1;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToTop();
                    _textView.MoveCaretToLine(_lastLineNumber - 1);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertLastLine(_lastLineNumber);
                }

                [WpfFact]
                public void ScrollBelowBottom()
                {
                    _globalSettings.ScrollOffset = 2;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToTop();
                    _textView.MoveCaretToLine(_lastLineNumber - 1);
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertLastLine(_lastLineNumber);
                }

                [WpfFact]
                public void OverFold()
                {
                    _globalSettings.ScrollOffset = 2;
                    _textView.SetVisibleLineCount(5);
                    _textView.ScrollToTop();
                    var mouseLineNumber = _lastLineNumber - 6;
                    _textView.MoveCaretToLine(mouseLineNumber);
                    _foldManager.CreateFold(_textBuffer.GetLineRange(startLine: mouseLineNumber + 1, endLine: mouseLineNumber + 4));
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertLastLine(mouseLineNumber + 5);
                }
            }

            public sealed class MiscScrollOffsetTest : ScrollOffsetTest
            {
                [WpfFact]
                public void SingleLineSingleOffset()
                {
                    _textBuffer.SetText("");
                    _textView.MoveCaretToLine(0);
                    _globalSettings.ScrollOffset = 1;
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(0);
                }

                [WpfFact]
                public void SingleLineBigOffset()
                {
                    _textBuffer.SetText("");
                    _textView.MoveCaretToLine(0);
                    _globalSettings.ScrollOffset = 100;
                    _commonOperationsRaw.AdjustTextViewForScrollOffset();
                    AssertFirstLine(0);
                }
            }
        }

        public sealed class VirtualEditTest : CommonOperationsIntegrationTest
        {
            /// <summary>
            /// If the caret is in the virtualedit=onemore the caret should remain in the line break
            /// </summary>
            [WpfFact]
            public void VirtualEditOneMore()
            {
                Create("cat", "dog");
                _globalSettings.VirtualEdit = "onemore";
                _textView.MoveCaretTo(3);
                _commonOperationsRaw.AdjustCaretForVirtualEdit();
                Assert.Equal(3, _textView.GetCaretPoint().Position);
            }

            /// <summary>
            /// If the caret is in default virtual edit then we should be putting the caret back in the 
            /// line
            /// </summary>
            [WpfFact]
            public void VirtualEditNormal()
            {
                Create("cat", "dog");
                _textView.MoveCaretTo(3);
                _commonOperationsRaw.AdjustCaretForVirtualEdit();
                Assert.Equal(2, _textView.GetCaretPoint().Position);
            }

            /// <summary>
            /// If the caret is in the selection exclusive and we're in visual mode then we should leave
            /// the caret in the line break.  It's needed to let motions like v$ get the appropriate 
            /// selection
            /// </summary>
            [WpfFact]
            public void ExclusiveSelectionAndVisual()
            {
                Create("cat", "dog");
                _globalSettings.Selection = "old";
                Assert.Equal(SelectionKind.Exclusive, _globalSettings.SelectionKind);

                foreach (var modeKind in new[] { ModeKind.VisualBlock, ModeKind.VisualCharacter, ModeKind.VisualLine })
                {
                    _vimBuffer.SwitchMode(modeKind, ModeArgument.None);
                    _textView.MoveCaretTo(3);
                    _commonOperationsRaw.AdjustCaretForVirtualEdit();
                    Assert.Equal(3, _textView.GetCaretPoint().Position);
                }
            }

            /// <summary>
            /// In a non-visual mode setting the exclusive selection setting shouldn't be a factor
            /// </summary>
            [WpfFact]
            public void ExclusiveSelectionOnly()
            {
                Create("cat", "dog");
                _textView.MoveCaretTo(3);
                _globalSettings.Selection = "old";
                Assert.Equal(SelectionKind.Exclusive, _globalSettings.SelectionKind);
                _commonOperationsRaw.AdjustCaretForVirtualEdit();
                Assert.Equal(2, _textView.GetCaretPoint().Position);
            }
        }

        public abstract class NormalizeBlanksAtColumnTest : CommonOperationsIntegrationTest
        {
            public sealed class NoExpandTab : NormalizeBlanksAtColumnTest
            {
                public NoExpandTab()
                {
                    Create("");
                    _vimBuffer.LocalSettings.ExpandTab = false;
                    _vimBuffer.LocalSettings.TabStop = 4;
                }

                [WpfFact]
                public void Simple()
                {
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 8), _textBuffer.GetColumnFromPosition(0));
                    Assert.Equal("\t\t", text);
                }

                [WpfFact]
                public void ExtraSpacesAtEnd()
                {
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 6), _textBuffer.GetColumnFromPosition(0));
                    Assert.Equal("\t  ", text);
                }

                [WpfFact]
                public void NonTabBoundary()
                {
                    _textBuffer.SetText("a");
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 8), _textBuffer.GetColumnFromPosition(1));
                    Assert.Equal("\t\t ", text);
                }

                [WpfFact]
                public void NonTabBoundaryExactTabPlusTab()
                {
                    _textBuffer.SetText("a");
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 7), _textBuffer.GetColumnFromPosition(1));
                    Assert.Equal("\t\t", text);
                }

                [WpfFact]
                public void NonTabBoundaryExactTab()
                {
                    _textBuffer.SetText("a");
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 3), _textBuffer.GetColumnFromPosition(1));
                    Assert.Equal("\t", text);
                }

                [WpfFact]
                public void NotEnoughSpaces()
                {
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 3), _textBuffer.GetColumnFromPosition(0));
                    Assert.Equal("   ", text);
                }

                [WpfFact]
                public void NonTabBoundaryWithTabs()
                {
                    _textBuffer.SetText("a");
                    var text = _commonOperations.NormalizeBlanksAtColumn("\t\t", _textBuffer.GetColumnFromPosition(1));
                    Assert.Equal("\t\t", text);
                }
            }

            public sealed class ExpandTab : NormalizeBlanksAtColumnTest
            {
                public ExpandTab()
                {
                    Create("");
                    _vimBuffer.LocalSettings.ExpandTab = true;
                    _vimBuffer.LocalSettings.TabStop = 4;
                }

                [WpfFact]
                public void ExactToTabBoundary()
                {
                    _textBuffer.SetText("a");
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 3), _textBuffer.GetColumnFromPosition(1));
                    Assert.Equal(new string(' ', 3), text);
                }

                [WpfFact]
                public void OneOverTabBoundary()
                {
                    _textBuffer.SetText("a");
                    var text = _commonOperations.NormalizeBlanksAtColumn(new string(' ', 4), _textBuffer.GetColumnFromPosition(1));
                    Assert.Equal(new string(' ', 4), text);
                }
            }
        }

        public sealed class GetSpacesToColumnTest : CommonOperationsIntegrationTest
        {
            [WpfFact]
            public void Simple()
            {
                Create("cat");
                Assert.Equal(2, _commonOperations.GetSpacesToColumn(_textBuffer.GetColumnFromPosition(2)));
            }

            /// <summary>
            /// Tabs on a 'tabstop' boundary are equivalent to 'tabstop' spaces
            /// </summary>
            [WpfFact]
            public void AfterTab()
            {
                Create("\tcat");
                _vimBuffer.LocalSettings.TabStop = 20;
                Assert.Equal(20, _commonOperations.GetSpacesToColumn(_textBuffer.GetColumnFromPosition(1)));
            }

            /// <summary>
            /// A tab which exists on a non-tabstop boundary only counts for the number of spaces remaining
            /// until the next tabstop boundary
            /// </summary>
            [WpfFact]
            public void AfterMixedTab()
            {
                Create("a\tcat");
                _vimBuffer.LocalSettings.TabStop = 4;
                Assert.Equal(4, _commonOperations.GetSpacesToColumn(_textBuffer.GetColumnFromPosition(2)));
            }

            [WpfFact]
            public void SurrogatePair()
            {
                const string alien = "\U0001F47D"; // 👽
                Create($"{alien}o{alien}");
                Assert.Equal(2, _commonOperations.GetSpacesToColumn(_textBuffer.GetColumnFromPosition(2)));
                Assert.Equal(3, _commonOperations.GetSpacesToColumn(_textBuffer.GetColumnFromPosition(3)));
            }

            [WpfFact]
            public void WideCharacter()
            {
                Create($"\u115fot");
                Assert.Equal(2, _commonOperations.GetSpacesToColumn(_textBuffer.GetColumnFromPosition(1)));
            }
        }

        public sealed class MiscTest : CommonOperationsIntegrationTest
        {
            [WpfFact]
            public void ViewFlagsValues()
            {
                Assert.Equal(ViewFlags.Standard, ViewFlags.Visible | ViewFlags.TextExpanded | ViewFlags.ScrollOffset);
                Assert.Equal(ViewFlags.All, ViewFlags.Visible | ViewFlags.TextExpanded | ViewFlags.ScrollOffset | ViewFlags.VirtualEdit);
            }
        }
    }
}

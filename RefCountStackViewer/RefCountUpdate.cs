using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;



namespace RefCountStackViewer
{
    public class RefCountInfo
    {
        public int LineNumber { get; protected set; }

        public IList<StackFrame> StackFrames { get; set; }

        public int RefCount { get; set; }

        public string TimeTravelPosition { get; set; }

        public ObservableCollection<RefCountInfo> Owner { get; set; }

    }

    public class RefCountGroup : RefCountInfo
    {
        public ObservableCollection<RefCountInfo> RefCountUpdates { get; set; }
        public string Description { get; set; }

        public int TotalInGroup
        {
            get
            {
                var total = 0;
                if (RefCountUpdates == null)
                    return 0;

                total = CountUpdatesInGroup(this);

                return total;
            }
        }

        private int CountUpdatesInGroup(RefCountGroup refCountGroup)
        {
            var total = 0;
            foreach (var info in refCountGroup.RefCountUpdates)
            {
                if (info is RefCountUpdate)
                    total++;
                else
                    total += CountUpdatesInGroup(info as RefCountGroup);
            }

            return total;
        }
    }

    public class RefCountUpdate : RefCountInfo
    {
        public RefCountUpdate(int lineNumber)
        {
            LineNumber = lineNumber;
        }
        internal static ObservableCollection<RefCountInfo> Parse(TextReader reader)
        {
            var refCountUpdates = new ObservableCollection<RefCountInfo>();
            var refCountUpdate = new RefCountUpdate(1) { Owner = refCountUpdates };

            var foundStart = false;
            var foundCount = false;
            var lineNumber = 0;
            string line = null;
            bool hasFrameLink = false;

            var retAddrIndex = -1;

            try
            {

                while (true)
                {
                    line = reader.ReadLine();
                    if (line == null)
                    {
                        // Add the one that was in progress
                        if (foundStart)
                        {
                            refCountUpdates.Add(refCountUpdate);
                        }

                        // Return the completed list
                        return refCountUpdates;
                    }

                    lineNumber++;
                    line = line.Trim();

                    // Blank line signals a gap between traces
                    if (line == "")
                    {
                        if (foundStart)
                        {
                            foundStart = foundCount = false;
                            refCountUpdates.Add(refCountUpdate);
                            refCountUpdate = new RefCountUpdate(lineNumber)
                            {
                                Owner = refCountUpdates
                            };
                        }

                        continue;
                    }

                    // If it's not a blank line, but we just had one, then we're in a new trace
                    else if (!foundStart)
                    {
                        foundStart = true;
                    }

                    if (!foundCount)
                    {
                        // Sometimes there's extra whitespace for alignment, but that makes string.Split(' ') not
                        // work.  So compress the whitespace
                        while (true)
                        {
                            var update = line.Replace("  ", " ");
                            if (update == line)
                                break;
                            line = update;
                        }

                        // The line with the count just has two numbers, separated by a space
                        var parts = line.Split(' ');
                        if (parts.Length != 2)
                        {
                            // Not the count
                            continue;
                        }

                        // If we get here it's probably a count
                        int count = 0;
                        if (int.TryParse(parts[1], NumberStyles.HexNumber, null, out count))
                            refCountUpdate.RefCount = count;
                        else
                            // Not a number, so not a count
                            continue;

                        foundCount = true;
                        continue;
                    }

                    // "!position -c" in the trace is optional, but check for it
                    const string timeTravelPosition = "Time Travel Position:";
                    if (line.StartsWith(timeTravelPosition))
                    {
                        refCountUpdate.TimeTravelPosition = line;
                        continue;
                    }

                    // Ignore debugger noise
                    var noise = false;
                    foreach (var prefix in _ignorableLinePrefixes)
                    {
                        if (line.StartsWith(prefix))
                        {
                            noise = true;
                            break;
                        }
                    }

                    if(noise)
                    {
                        continue;
                    }

                    // If we're in the stack area, add this frame
                    if (refCountUpdate.StackFrames != null)
                    {
                        var stackFrame = new StackFrame();

                        // ntsd e.g.: 04689864 55895b5d windows_ui_xaml!_InlineInterlockedCompareExchangePointer+0x12
                        // e.g. (?):  16 058fe6dc 65710f37 Windows_UI_Xaml!CDOCollection::LeaveImpl+0x1c2 [onecoreuap\windows\dxaml\xcp\components\collection\docollection.cpp @ 208] 
                        // ntsd e.g.: (Inline Function) --------`-------- windows_ui_xaml!DirectUI::UIElementGenerated::{ctor}+0x5
                        // windbg eg: 00 000000cc`412fc0f8 00007ffd`b53fb4ce windows_ui_xaml!DirectUI::DependencyObject::DependencyObject(void)+0x8
                        // using kc:  00 CoreCLR!__InterlockedIncrement64
                        // vs eg: 	  Office.UI.Xaml.OneNote.dll!__abi_FTMWeakRefData::Decrement
                        //            [Inline Frame] Windows.UI.Xaml.dll!ctl::release_interface_nonull

                        int ebpStart = hasFrameLink ? 3 : 0;

                        if (retAddrIndex != -1)
                        {
                            stackFrame.Ebp = line.Substring(ebpStart, retAddrIndex - 1);
                        }

                        if (retAddrIndex != -1)
                        {
                            var sub = line.Substring(retAddrIndex).Trim();
                            var firstSpace = sub.IndexOf(' ');
                            Debug.Assert(firstSpace != -1);
                            if (firstSpace == -1)
                            {
                                throw new Exception("Invalid stack frame");
                            }
                            stackFrame.Method = sub.Substring(firstSpace + 1).Trim();
                        }
                        else
                        {
                            var parts = line.Split(' ');

                            bool firstPartIsANumber = int.TryParse(parts[0], NumberStyles.AllowHexSpecifier, null, out var number);

                            if (!firstPartIsANumber)
                            {
                                // vs case
                                stackFrame.Method = line;
                            }
                            else
                            {
                                // kc case
                                stackFrame.Method = parts[1];
                            }
                        }

                        var subParts = stackFrame.Method.Split('+');
                        if (subParts.Length == 2)
                        {
                            stackFrame.Method = subParts[0];
                            stackFrame.Offset = subParts[1];
                        }

                        refCountUpdate.StackFrames.Add(stackFrame);
                    }

                    // See if this is the start of the stack
                    else
                    {
                        // The header line for a call stack in various modes of various debuggers.
                        // Spaces compressed out to make the check easier.
                        var stackHeaders = new List<string>()
                        {
                            "ChildEBPRetAddr",
                            "#ChildEBPRetAddr",
                            "Child-SPRetAddrCallSite",
                            "#Child-SPRetAddrCallSite"
                        };

                        // Is this the header row for a stack?
                        if(stackHeaders.Contains(line.Replace(" ","")))
                        {
                            // Yes, create a new list of stack frames

                            refCountUpdate.StackFrames = new List<StackFrame>();

                            retAddrIndex = line.IndexOf("RetAddr");

                            if (line.StartsWith("# "))
                            {
                                // This helps us find the ebp later
                                // bugbug: can't remember what this is about, show some sample lines
                                hasFrameLink = true;
                            }

                        }

                        // VS just shows a stack frame.  Make a guess
                        else if (line.Contains("!"))
                        {
                            refCountUpdate.StackFrames = new List<StackFrame>();
                            refCountUpdate.StackFrames.Add(new StackFrame() { Method = line });
                        }
                    }

                }
            }
            catch (Exception)
            {
                MessageBox.Show("Parsing error on line " + lineNumber.ToString() + "\n" + line);
                return null;
            }
        }

        static string[] _ignorableLinePrefixes =
        {
            "ModLoad:",
            "WARNING:",
            "ERROR:",
            "*** WARNING:",
            "*** ERROR:",
            "TTD:"
        };


    }
}

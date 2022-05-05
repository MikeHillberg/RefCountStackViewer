using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RefCountStackViewer
{
    internal static class SampleData
    {
        internal static string DumpOutput = @"


31c57098 00000001
Time Travel Position:  AAAAAAAA in Process 0
Garbage
ChildEBP RetAddr  
04c8d9c4 56593531 windows_ui_xaml!Init
04c8da54 55c60387 windows_ui_xaml!Create
04c8da80 55c64c31 windows_ui_xaml!CFxCallbacks::FrameworkCallbacks_ReferenceTrackerWalk+0x47 [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\core\inc\fxcallbacks.g.h @ 1358]


31c57098 00000002
Time Travel Position:  AAAAAAAA in Process 0
Garbage
ChildEBP RetAddr  
04c8d9c4 56593531 windows_ui_xaml!Add
04c8da34 5659340f windows_ui_xaml!Caller1
04c8da54 55c60387 windows_ui_xaml!Caller2
04c8da80 55c64c31 windows_ui_xaml!CFxCallbacks::FrameworkCallbacks_ReferenceTrackerWalk+0x47 [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\core\inc\fxcallbacks.g.h @ 1358]

31c57098 00000001
Time Travel Position:  AAAAAAAA in Process 0
Garbage
ChildEBP RetAddr  
04c8d9c4 56593531 windows_ui_xaml!Release
04c8da34 5659340f windows_ui_xaml!Caller1
04c8da54 55c60387 windows_ui_xaml!Caller2
04c8da80 55c64c31 windows_ui_xaml!CFxCallbacks::FrameworkCallbacks_ReferenceTrackerWalk+0x47 [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\core\inc\fxcallbacks.g.h @ 1358]

31c57098 00000002
Time Travel Position:  AAAAAAAA in Process 0
Garbage
ChildEBP RetAddr  
04c8d9c4 56593531 windows_ui_xaml!Add
04c8da54 55c60387 windows_ui_xaml!Caller2
04c8da80 55c64c31 windows_ui_xaml!CFxCallbacks::FrameworkCallbacks_ReferenceTrackerWalk+0x47 [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\core\inc\fxcallbacks.g.h @ 1358]


31c57098 00000001
Time Travel Position:  AAAAAAAA in Process 0
Garbage
ChildEBP RetAddr  
04c8d9c4 56593531 windows_ui_xaml!Release
04c8da54 55c60387 windows_ui_xaml!Caller2
04c8da80 55c64c31 windows_ui_xaml!CFxCallbacks::FrameworkCallbacks_ReferenceTrackerWalk+0x47 [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\core\inc\fxcallbacks.g.h @ 1358]


31c57098 00000001
Time Travel Position:  AAAAAAAA in Process 0
Garbage
ChildEBP RetAddr  
04c8d9c4 56593531 windows_ui_xaml!Add
04c8da34 5659340f windows_ui_xaml!DirectUI::DependencyObject::ReferenceTrackerWalk+0x101 [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\dxaml\lib\dependencyobject.cpp @ 3175]
04c8da54 55c60387 windows_ui_xaml!DirectUI::DependencyObject::ReferenceTrackerWalk+0x7f [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\dxaml\lib\dependencyobject.cpp @ 3840]
04c8da80 55c64c31 windows_ui_xaml!CFxCallbacks::FrameworkCallbacks_ReferenceTrackerWalk+0x47 [f:\sd\rs1\onecoreuap\windows\dxaml\xcp\core\inc\fxcallbacks.g.h @ 1358]



";
    }
}

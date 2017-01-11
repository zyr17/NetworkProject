﻿#pragma checksum "..\..\ConnectWindow.xaml" "{406ea660-64cf-4c82-b6f0-42d48172a799}" "D234728D044F3AFAA7F4551BA80FDEBE"
//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.34209
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------

using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Media.TextFormatting;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Shell;


namespace NetworkProject {
    
    
    /// <summary>
    /// ConnectWindow
    /// </summary>
    public partial class ConnectWindow : System.Windows.Window, System.Windows.Markup.IComponentConnector {
        
        
        #line 10 "..\..\ConnectWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Image ShowImage;
        
        #line default
        #line hidden
        
        
        #line 12 "..\..\ConnectWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Border UpperBorder;
        
        #line default
        #line hidden
        
        
        #line 21 "..\..\ConnectWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Grid FileStatusGrid;
        
        #line default
        #line hidden
        
        
        #line 28 "..\..\ConnectWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.ProgressBar FileProgressBar;
        
        #line default
        #line hidden
        
        
        #line 31 "..\..\ConnectWindow.xaml"
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1823:AvoidUnusedPrivateFields")]
        internal System.Windows.Controls.Button FullScreenButton;
        
        #line default
        #line hidden
        
        private bool _contentLoaded;
        
        /// <summary>
        /// InitializeComponent
        /// </summary>
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        public void InitializeComponent() {
            if (_contentLoaded) {
                return;
            }
            _contentLoaded = true;
            System.Uri resourceLocater = new System.Uri("/NetworkProject;component/connectwindow.xaml", System.UriKind.Relative);
            
            #line 1 "..\..\ConnectWindow.xaml"
            System.Windows.Application.LoadComponent(this, resourceLocater);
            
            #line default
            #line hidden
        }
        
        [System.Diagnostics.DebuggerNonUserCodeAttribute()]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("PresentationBuildTasks", "4.0.0.0")]
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        [System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1800:DoNotCastUnnecessarily")]
        void System.Windows.Markup.IComponentConnector.Connect(int connectionId, object target) {
            switch (connectionId)
            {
            case 1:
            
            #line 4 "..\..\ConnectWindow.xaml"
            ((NetworkProject.ConnectWindow)(target)).KeyDown += new System.Windows.Input.KeyEventHandler(this.ConnectWindow_KeyDown);
            
            #line default
            #line hidden
            
            #line 4 "..\..\ConnectWindow.xaml"
            ((NetworkProject.ConnectWindow)(target)).KeyUp += new System.Windows.Input.KeyEventHandler(this.ConnectWindow_KeyUp);
            
            #line default
            #line hidden
            
            #line 4 "..\..\ConnectWindow.xaml"
            ((NetworkProject.ConnectWindow)(target)).Closing += new System.ComponentModel.CancelEventHandler(this.Window_Closing);
            
            #line default
            #line hidden
            return;
            case 2:
            this.ShowImage = ((System.Windows.Controls.Image)(target));
            
            #line 10 "..\..\ConnectWindow.xaml"
            this.ShowImage.MouseMove += new System.Windows.Input.MouseEventHandler(this.ShowImage_MouseMove);
            
            #line default
            #line hidden
            
            #line 10 "..\..\ConnectWindow.xaml"
            this.ShowImage.MouseDown += new System.Windows.Input.MouseButtonEventHandler(this.ShowImage_MouseDown);
            
            #line default
            #line hidden
            
            #line 10 "..\..\ConnectWindow.xaml"
            this.ShowImage.MouseUp += new System.Windows.Input.MouseButtonEventHandler(this.ShowImage_MouseUp);
            
            #line default
            #line hidden
            
            #line 10 "..\..\ConnectWindow.xaml"
            this.ShowImage.MouseWheel += new System.Windows.Input.MouseWheelEventHandler(this.ShowImage_MouseWheel);
            
            #line default
            #line hidden
            return;
            case 3:
            this.UpperBorder = ((System.Windows.Controls.Border)(target));
            
            #line 12 "..\..\ConnectWindow.xaml"
            this.UpperBorder.MouseEnter += new System.Windows.Input.MouseEventHandler(this.Border_MouseEnter);
            
            #line default
            #line hidden
            
            #line 12 "..\..\ConnectWindow.xaml"
            this.UpperBorder.MouseLeave += new System.Windows.Input.MouseEventHandler(this.Border_MouseLeave);
            
            #line default
            #line hidden
            return;
            case 4:
            
            #line 19 "..\..\ConnectWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.UploadButtonClick);
            
            #line default
            #line hidden
            return;
            case 5:
            
            #line 20 "..\..\ConnectWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.DownloadButtonClick);
            
            #line default
            #line hidden
            return;
            case 6:
            this.FileStatusGrid = ((System.Windows.Controls.Grid)(target));
            return;
            case 7:
            this.FileProgressBar = ((System.Windows.Controls.ProgressBar)(target));
            return;
            case 8:
            
            #line 29 "..\..\ConnectWindow.xaml"
            ((System.Windows.Controls.Button)(target)).Click += new System.Windows.RoutedEventHandler(this.CancelButtonClick);
            
            #line default
            #line hidden
            return;
            case 9:
            this.FullScreenButton = ((System.Windows.Controls.Button)(target));
            
            #line 31 "..\..\ConnectWindow.xaml"
            this.FullScreenButton.Click += new System.Windows.RoutedEventHandler(this.FullScreenButtonClick);
            
            #line default
            #line hidden
            return;
            }
            this._contentLoaded = true;
        }
    }
}


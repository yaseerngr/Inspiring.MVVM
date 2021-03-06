﻿namespace Inspiring.Mvvm.Views {
   using System;
   using System.Collections.Generic;
   using System.Linq;
   using System.Windows;
   using Inspiring.Mvvm.Common;
   using Inspiring.Mvvm.Screens;
   using Microsoft.Win32;

   public class DialogService : IDialogService {
      private readonly EventAggregator _aggregator;
      private readonly IWindowService _windowService;

      public DialogService(EventAggregator aggregator, IWindowService windowService) {
         Check.NotNull(aggregator, nameof(aggregator));
         Check.NotNull(windowService, nameof(windowService));

         _aggregator = aggregator;
         _windowService = windowService;
      }

      /// <remarks>
      ///   Note to inheritors: This method does not call <see 
      ///   cref="Show(Window,IScreenFactory{IScreenBase},bool)"/>.
      /// </remarks>
      public virtual void Show(
         IScreenFactory<IScreenBase> screen,
         IScreenBase parent = null,
         string title = null
      ) {
         Window ownerWin = GetAssociatedWindow(parent);
         Window window = _windowService.CreateWindow(ownerWin, title, false);

         WindowLifecycle.Show(
            _aggregator,
            _windowService,
            window,
            screen,
            modal: false
         );
      }

      /// <remarks>
      ///   Note to inheritors: This method does not call <see 
      ///   cref="Show(Window, IScreenFactory{IScreenBase}, bool)"/>.
      /// </remarks>
      public virtual DialogScreenResult ShowDialog(
         IScreenFactory<IScreenBase> screen,
         IScreenBase parent = null,
         string title = null
      ) {
         Window ownerWin = GetAssociatedWindow(parent);
         Window window = _windowService.CreateWindow(ownerWin, title, true);

         return WindowLifecycle.Show(
            _aggregator,
            _windowService,
            window,
            screen,
            modal: true
         );
      }

      public virtual DialogScreenResult Show(
         Window window,
         IScreenFactory<IScreenBase> screen,
         bool modal
      ) {
         return WindowLifecycle.Show(
            _aggregator,
            _windowService,
            window,
            screen,
            modal
         );
      }

      public virtual bool ShowOpenFileDialog(
         IScreenBase parent,
         out string fileName,
         string filter = null,
         string initialDirectory = null,
         IEnumerable<string> customPlaces = null
      ) {
         Window ownerWin = GetAssociatedWindow(parent);
         OpenFileDialog ofd = new OpenFileDialog();

         if (filter != null) {
            ofd.Filter = filter;
         }

         if (initialDirectory != null) {
            ofd.InitialDirectory = initialDirectory;
         }

         if (customPlaces != null) {
            foreach (string customPlace in customPlaces) {
               ofd.CustomPlaces.Add(new FileDialogCustomPlace(customPlace));
            }
         }

         var result = ofd.ShowDialog(ownerWin);
         fileName = ofd.FileName;
         return result.Value;
      }

      public virtual bool ShowSaveFileDialog(
         IScreenBase parent,
         ref string fileName,
         string filter = null,
         string initialDirectory = null,
         IEnumerable<string> customPlaces = null
      ) {
         Window ownerWin = GetAssociatedWindow(parent);
         SaveFileDialog sfd = new SaveFileDialog();
         sfd.FileName = fileName;

         if (filter != null) {
            sfd.Filter = filter;
         }

         if (initialDirectory != null) {
            sfd.InitialDirectory = initialDirectory;
         }

         if (customPlaces != null) {
            foreach (string customPlace in customPlaces) {
               sfd.CustomPlaces.Add(new FileDialogCustomPlace(customPlace));
            }
         }

         var result = sfd.ShowDialog(ownerWin);
         fileName = sfd.FileName;
         return result.Value;
      }

      public virtual bool ShowFolderBrowseDialog(
         IScreenBase parent,
         out string selectedPath,
         string message,
         Environment.SpecialFolder? specialFolder = null
      ) {
         Window ownerWin = GetAssociatedWindow(parent);

         FolderBrowserDialog fbd = new FolderBrowserDialog();
         if (specialFolder != null) {
            fbd.RootFolder = (Environment.SpecialFolder)specialFolder;
         }

         fbd.Description = message;

         bool result = fbd.ShowDialog(ownerWin);

         selectedPath = result ? fbd.SelectedPath : String.Empty;

         return result;
      }

      public Window GetAssociatedWindow(IScreenBase screen) {
         if (screen == null) {
            return null;
         }

         WindowLifecycle lf = ScreenTreeHelper
            .GetAncestorsOf(screen, includeSelf: true)
            .SelectMany(s => s.Children.OfType<WindowLifecycle>())
            .FirstOrDefault();

         if (lf != null) {
            return lf.Window;
         }

         throw new ArgumentException(ExceptionTexts.NoAssociatedWindow);
      }

      public virtual void Info(string message, string caption) {
         MessageBox.Show(
            message,
            caption,
            MessageBoxButton.OK,
            MessageBoxImage.Information
         );
      }

      public virtual void Warning(string message, string caption) {
         MessageBox.Show(
            message,
            caption,
            MessageBoxButton.OK,
            MessageBoxImage.Warning
         );
      }

      public virtual void Error(string message, string caption) {
         MessageBox.Show(
            message,
            caption,
            MessageBoxButton.OK,
            MessageBoxImage.Error
         );
      }

      public virtual CustomDialogResult YesNo(
         string message,
         string caption,
         CustomDialogResult defaultResult,
         CustomDialogIcon icon = CustomDialogIcon.Question
      ) {
         return MapResult(
            MessageBox.Show(
               message,
               caption,
               MessageBoxButton.YesNo,
               MapIcon(icon),
               MapResult(defaultResult)
            )
         );
      }

      public virtual CustomDialogResult YesNoCancel(
         string message,
         string caption,
         CustomDialogResult defaultResult,
         CustomDialogIcon icon = CustomDialogIcon.Question
      ) {
         return MapResult(
            MessageBox.Show(
               message,
               caption,
               MessageBoxButton.YesNoCancel,
               MapIcon(icon),
               MapResult(defaultResult)
            )
         );
      }

      public virtual CustomDialogResult OkCancel(
         string message,
         string caption,
         CustomDialogResult defaultResult,
         CustomDialogIcon icon = CustomDialogIcon.Information
      ) {
         return MapResult(
            MessageBox.Show(
               message,
               caption,
               MessageBoxButton.OKCancel,
               MapIcon(icon),
               MapResult(defaultResult)
            )
         );
      }

      protected static MessageBoxImage MapIcon(CustomDialogIcon icon) {
         switch (icon) {
            case CustomDialogIcon.None:
               return MessageBoxImage.None;
            case CustomDialogIcon.Error:
               return MessageBoxImage.Error;
            case CustomDialogIcon.Hand:
               return MessageBoxImage.Hand;
            case CustomDialogIcon.Stop:
               return MessageBoxImage.Stop;
            case CustomDialogIcon.Question:
               return MessageBoxImage.Question;
            case CustomDialogIcon.Exclamation:
               return MessageBoxImage.Exclamation;
            case CustomDialogIcon.Warning:
               return MessageBoxImage.Warning;
            case CustomDialogIcon.Information:
               return MessageBoxImage.Information;
            case CustomDialogIcon.Asterisk:
               return MessageBoxImage.Asterisk;
            default:
               throw new ArgumentException();
         }
      }

      protected static MessageBoxResult MapResult(CustomDialogResult result) {
         switch (result) {
            case CustomDialogResult.Cancel:
               return MessageBoxResult.Cancel;
            case CustomDialogResult.No:
               return MessageBoxResult.No;
            case CustomDialogResult.None:
               return MessageBoxResult.None;
            case CustomDialogResult.OK:
               return MessageBoxResult.OK;
            case CustomDialogResult.Yes:
               return MessageBoxResult.Yes;
            default:
               throw new ArgumentException();
         }
      }

      protected static CustomDialogResult MapResult(MessageBoxResult result) {
         switch (result) {
            case MessageBoxResult.Cancel:
               return CustomDialogResult.Cancel;
            case MessageBoxResult.No:
               return CustomDialogResult.No;
            case MessageBoxResult.None:
               return CustomDialogResult.None;
            case MessageBoxResult.OK:
               return CustomDialogResult.OK;
            case MessageBoxResult.Yes:
               return CustomDialogResult.Yes;
            default:
               throw new ArgumentException();
         }
      }
   }
}

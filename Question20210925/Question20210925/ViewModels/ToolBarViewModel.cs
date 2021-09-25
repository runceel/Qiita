using boilersGraphics.Controls;
using boilersGraphics.Extensions;
using boilersGraphics.Helpers;
using boilersGraphics.Models;
using boilersGraphics.Views.Behaviors;
using Microsoft.Xaml.Behaviors;
using Prism.Commands;
using Prism.Services.Dialogs;
using Question20210925;
using Reactive.Bindings;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;

namespace boilersGraphics.ViewModels
{
    public class ToolBarViewModel
    {
        private IDialogService dlgService = null;
        public ObservableCollection<ToolItemData> ToolItems { get; } = new ObservableCollection<ToolItemData>();

        public BehaviorCollection Behaviors { get { return Interaction.GetBehaviors(App.Current.MainWindow.GetChildOfType<DesignerCanvas>()); } }

        public ReactivePropertySlim<bool> CurrentHitTestVisibleState { get; } = new ReactivePropertySlim<bool>();

        public ToolBarViewModel(IDialogService dialogService)
        {
            this.dlgService = dialogService;
            ToolItems.Add(new ToolItemData("pointer", "pack://application:,,,/Assets/img/pointer.png", new DelegateCommand(() =>
            {
                var deselectBehavior = new DeselectBehavior();
                Behaviors.Clear();
                if (!Behaviors.Contains(deselectBehavior))
                {
                    Behaviors.Add(deselectBehavior);
                }
                ChangeHitTestToEnable();
                SelectOneToolItem("pointer");
            })));
            ToolItems.Add(new ToolItemData("rubberband", "pack://application:,,,/Assets/img/rubberband.png", new DelegateCommand(() =>
            {
                var behavior = new RubberbandBehavior();
                Behaviors.Clear();
                if (!Behaviors.Contains(behavior))
                {
                    Behaviors.Add(behavior);
                }
                ChangeHitTestToEnable();
                SelectOneToolItem("rubberband");
            })));
            ToolItems.Add(new ToolItemData("straightline", "pack://application:,,,/Assets/img/straightline.png", new DelegateCommand(() =>
            {
                var behavior = new NDrawStraightLineBehavior();
                Behaviors.Clear();
                if (!Behaviors.Contains(behavior))
                {
                    Behaviors.Add(behavior);
                }
                ChangeHitTestToDisable();
                SelectOneToolItem("straightline");
            })));
            
        }

        private void ChangeHitTestToDisable()
        {
            var diagramViewModel = (App.Current.MainWindow.DataContext as MainWindowViewModel).DiagramViewModel;
            diagramViewModel.AllItems.Value.ToList().ForEach(x => x.IsHitTestVisible.Value = false);
            CurrentHitTestVisibleState.Value = false;
        }

        private void ChangeHitTestToEnable()
        {
            var diagramViewModel = (App.Current.MainWindow.DataContext as MainWindowViewModel).DiagramViewModel;
            diagramViewModel.SelectedLayers.Value.ToList().ForEach(x => 
                (x as Layer).Children.ToList().ForEach(y =>
                {
                    var layerItem = y as LayerItem;
                    layerItem.Item.Value.IsHitTestVisible.Value = true;
                    Trace.WriteLine($"{layerItem.Name.Value}.IsHitTestVisible={layerItem.Item.Value.IsHitTestVisible.Value}");
                })
            );
            CurrentHitTestVisibleState.Value = true;
        }

        private void SelectOneToolItem(string toolName)
        {
            var toolItem = ToolItems.Where(i => i.Name == toolName).Single();
            toolItem.IsChecked = true;

            ToolItems.Where(i => i.Name != toolName).ToList().ForEach(i => i.IsChecked = false);
        }
    }
}

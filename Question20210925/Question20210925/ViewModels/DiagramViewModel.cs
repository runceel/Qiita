using boilersGraphics.Controls;
using boilersGraphics.Extensions;
using boilersGraphics.Helpers;
using boilersGraphics.Messenger;
using boilersGraphics.Models;
using boilersGraphics.UserControls;
using Microsoft.Win32;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using Question20210925;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Xml;
using System.Xml.Linq;
using TsOperationHistory.Extensions;

namespace boilersGraphics.ViewModels
{
    public class DiagramViewModel : BindableBase, IDiagramViewModel, IDisposable
    {
        public MainWindowViewModel MainWindowVM { get; private set; }
        private IDialogService dlgService;
        private Point _CurrentPoint;
        private ObservableCollection<Color> _EdgeColors = new ObservableCollection<Color>();
        private ObservableCollection<Color> _FillColors = new ObservableCollection<Color>();
        private CompositeDisposable _CompositeDisposable = new CompositeDisposable();
        private int _Width;
        private int _Height;
        private double _CanvasBorderThickness;
        private bool _MiddleButtonIsPressed;
        private Point _MousePointerPosition;
        private bool disposedValue;

        public DelegateCommand<object> AddItemCommand { get; private set; }
        public DelegateCommand<object> RemoveItemCommand { get; private set; }
        public DelegateCommand<object> ClearSelectedItemsCommand { get; private set; }
        public DelegateCommand SelectAllCommand { get; private set; }
        public DelegateCommand UndoCommand { get; private set; }
        public DelegateCommand RedoCommand { get; private set; }
        public DelegateCommand<MouseWheelEventArgs> MouseWheelCommand { get; private set; }
        public DelegateCommand<MouseEventArgs> PreviewMouseDownCommand { get; private set; }
        public DelegateCommand<MouseEventArgs> PreviewMouseUpCommand { get; private set; }
        public DelegateCommand<MouseEventArgs> MouseMoveCommand { get; private set; }
        public DelegateCommand<MouseEventArgs> MouseLeaveCommand { get; private set; }
        public DelegateCommand<MouseEventArgs> MouseEnterCommand { get; private set; }
        public DelegateCommand<KeyEventArgs> PreviewKeyDownCommand { get; private set; }

        #region Property

        public ReactivePropertySlim<LayerTreeViewItemBase> RootLayer { get; set; } = new ReactivePropertySlim<LayerTreeViewItemBase>(new LayerTreeViewItemBase());

        public ReactiveCollection<LayerTreeViewItemBase> Layers { get; }

        public ReadOnlyReactivePropertySlim<LayerTreeViewItemBase[]> SelectedLayers { get; }

        public ReadOnlyReactivePropertySlim<SelectableDesignerItemViewModelBase[]> AllItems { get; }

        public ReadOnlyReactivePropertySlim<SelectableDesignerItemViewModelBase[]> SelectedItems { get; }

        public ReactivePropertySlim<BackgroundViewModel> BackgroundItem { get; } = new ReactivePropertySlim<BackgroundViewModel>();

        public ReactiveProperty<double?> EdgeThickness { get; } = new ReactiveProperty<double?>();

        public ReactiveProperty<bool> EnableMiniMap { get; } = new ReactiveProperty<bool>();

        public ReactiveProperty<string> FileName { get; } = new ReactiveProperty<string>();

        public ReactiveProperty<Color> CanvasBackground { get; } = new ReactiveProperty<Color>();

        public ReactiveProperty<bool> EnablePointSnap { get; } = new ReactiveProperty<bool>();

        public ObservableCollection<Color> EdgeColors
        {
            get { return _EdgeColors; }
            set { SetProperty(ref _EdgeColors, value); }
        }

        public ObservableCollection<Color> FillColors
        {
            get { return _FillColors; }
            set { SetProperty(ref _FillColors, value); }
        }

        public int Width
        {
            get { return _Width; }
            set { SetProperty(ref _Width, value); }
        }

        public int Height
        {
            get { return _Height; }
            set { SetProperty(ref _Height, value); }
        }

        /// <summary>
        /// 現在ポインティングしている座標
        /// ステータスバー上の座標インジケーターに使用される
        /// </summary>
        public Point CurrentPoint
        {
            get { return _CurrentPoint; }
            set { SetProperty(ref _CurrentPoint, value); }
        }
        public double CanvasBorderThickness
        {
            get { return _CanvasBorderThickness; }
            set { SetProperty(ref _CanvasBorderThickness, value); }
        }

        public double ScaleX { get; set; } = 1.0;
        public double ScaleY { get; set; } = 1.0;
        public System.Version BGSXFileVersion { get; } = new System.Version(2, 1);

        public int LayerCount { get; set; } = 1;

        public int LayerItemCount { get; set; } = 1;

        public IEnumerable<Point> SnapPoints
        {
            get
            {
                var designerCanvas = App.Current.MainWindow.GetChildOfType<DesignerCanvas>();
                var resizeThumbs = designerCanvas.EnumerateChildOfType<SnapPoint>();
                var sets = resizeThumbs
                                .Select(x => new Tuple<SnapPoint, Point>(x, GetCenter(x)))
                                .Distinct();
                return sets.Select(x => x.Item2);
            }
        }

        public IEnumerable<Point> GetSnapPoints(IEnumerable<SnapPoint> exceptSnapPoints)
        {
            var designerCanvas = App.Current.MainWindow.GetChildOfType<DesignerCanvas>();
            var resizeThumbs = designerCanvas.EnumerateChildOfType<SnapPoint>();
            var sets = resizeThumbs
                            .Where(x => !exceptSnapPoints.Contains(x))
                            .Select(x => new Tuple<SnapPoint, Point>(x, GetCenter(x)))
                            .Distinct();
            return sets.Select(x => x.Item2);
        }

        #endregion //Property

        public DiagramViewModel(MainWindowViewModel mainWindowViewModel, int width, int height)
        {
            MainWindowVM = mainWindowViewModel;

            AddItemCommand = new DelegateCommand<object>(p => ExecuteAddItemCommand(p));
            RemoveItemCommand = new DelegateCommand<object>(p => ExecuteRemoveItemCommand(p));
            ClearSelectedItemsCommand = new DelegateCommand<object>(p => ExecuteClearSelectedItemsCommand(p));
            MouseWheelCommand = new DelegateCommand<MouseWheelEventArgs>(args =>
            {
            });
            PreviewMouseDownCommand = new DelegateCommand<MouseEventArgs>(args =>
            {
                if (args.MiddleButton == MouseButtonState.Pressed)
                {
                    _MiddleButtonIsPressed = true;
                    var diagramControl = App.Current.MainWindow.GetChildOfType<DiagramControl>();
                    _MousePointerPosition = args.GetPosition(diagramControl);
                    diagramControl.Cursor = Cursors.SizeAll;
                }
            });
            PreviewMouseUpCommand = new DelegateCommand<MouseEventArgs>(args =>
            {
                ReleaseMiddleButton(args);
            });
            MouseMoveCommand = new DelegateCommand<MouseEventArgs>(args =>
            {
                if (_MiddleButtonIsPressed)
                {
                    var diagramControl = App.Current.MainWindow.GetChildOfType<DiagramControl>();
                    var scrollViewer = diagramControl.GetChildOfType<ScrollViewer>();
                    var newMousePointerPosition = args.GetPosition(diagramControl);
                    var diff = newMousePointerPosition - _MousePointerPosition;
                    scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset - diff.Y);
                    scrollViewer.ScrollToHorizontalOffset(scrollViewer.HorizontalOffset - diff.X);
                    _MousePointerPosition = newMousePointerPosition;
                }
            });
            MouseLeaveCommand = new DelegateCommand<MouseEventArgs>(args =>
            {
                if (_MiddleButtonIsPressed)
                {
                    ReleaseMiddleButton(args);
                }
            });
            MouseEnterCommand = new DelegateCommand<MouseEventArgs>(args =>
            {
                if (_MiddleButtonIsPressed)
                {
                    ReleaseMiddleButton(args);
                }
            });
            PreviewKeyDownCommand = new DelegateCommand<KeyEventArgs>(args =>
            {
                switch (args.Key)
                {
                    case Key.Left:
                        MoveSelectedItems(-1, 0);
                        args.Handled = true;
                        break;
                    case Key.Up:
                        MoveSelectedItems(0, -1);
                        args.Handled = true;
                        break;
                    case Key.Right:
                        MoveSelectedItems(1, 0);
                        args.Handled = true;
                        break;
                    case Key.Down:
                        MoveSelectedItems(0, 1);
                        args.Handled = true;
                        break;
                }
            });


            EdgeColors.CollectionChangedAsObservable()
                .Subscribe(_ => RaisePropertyChanged("EdgeColors"))
                .AddTo(_CompositeDisposable);
            FillColors.CollectionChangedAsObservable()
                .Subscribe(_ => RaisePropertyChanged("FillColors"))
                .AddTo(_CompositeDisposable);

            Layers = RootLayer.Value.Children.CollectionChangedAsObservable()
                           .Select(_ => RootLayer.Value.LayerChangedAsObservable())
                           .Switch()
                           .SelectMany(_ => RootLayer.Value.Children)
                           .ToReactiveCollection();

            AllItems = Layers.CollectionChangedAsObservable()
                             .Select(_ => Layers.Select(x => x.LayerItemsChangedAsObservable()).Merge()
                                .Merge(this.ObserveProperty(y => y.BackgroundItem.Value).ToUnit()))
                             .Switch()
                             .Select(_ => Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                                .Where(x => x.GetType() == typeof(LayerItem))
                                                .Select(y => (y as LayerItem).Item.Value)
                                                .Union(new SelectableDesignerItemViewModelBase[] { BackgroundItem.Value })
                                                .ToArray())
                             .ToReadOnlyReactivePropertySlim(Array.Empty<SelectableDesignerItemViewModelBase>());

            AllItems.Subscribe(x =>
            {
                Trace.WriteLine($"{x.Length} items in AllItems.");
                Trace.WriteLine(string.Join(", ", x.Select(y => y?.ToString() ?? "null")));
            })
            .AddTo(_CompositeDisposable);

            SelectedItems = Layers.CollectionChangedAsObservable()
                                  .Select(_ => Layers.Select(x => x.SelectedLayerItemsChangedAsObservable()).Merge()
                                      .Merge(Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                                   .Where(x => x.GetType() == typeof(LayerItem))
                                                   .Select(y => (y as LayerItem).Item.Value)
                                                   .OfType<ConnectorBaseViewModel>()
                                                   .SelectMany(x => new List<SnapPointViewModel>() { x.SnapPoint0VM.Value, x.SnapPoint1VM.Value })
                                                   .ToObservableCollection()
                                                   .ObserveElementProperty(x => x.IsSelected.Value)
                                                   .ToUnit()))
                                  .Switch()
                                  .Select(_ => Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                                     .Where(x => x.GetType() == typeof(LayerItem))
                                                     .Select(y => (y as LayerItem).Item.Value)
                                                     .Except(Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                                                   .Where(x => x.GetType() == typeof(LayerItem))
                                                                   .Select(y => (y as LayerItem).Item.Value)
                                                                   .OfType<ConnectorBaseViewModel>())
                                                     .Union(Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                                                  .Where(x => x.GetType() == typeof(LayerItem))
                                                                  .Select(y => (y as LayerItem).Item.Value)
                                                                  .OfType<ConnectorBaseViewModel>()
                                                                  .SelectMany(x => new List<SnapPointViewModel>() { x.SnapPoint0VM.Value, x.SnapPoint1VM.Value })
                                                            )
                                                     .Where(z => z.IsSelected.Value == true)
                                                     .OrderBy(z => z.SelectedOrder.Value)
                                                     .ToArray())
                                  .ToReadOnlyReactivePropertySlim(Array.Empty<SelectableDesignerItemViewModelBase>());

            SelectedItems.Subscribe(selectedItems =>
            {
                Trace.WriteLine($"SelectedItems changed {string.Join(", ", selectedItems.Select(x => x?.ToString() ?? "null"))}");
            })
            .AddTo(_CompositeDisposable);

            SelectedLayers = Layers.ObserveElementObservableProperty(x => x.IsSelected)
                                   .Select(_ => Layers.Where(x => x.IsSelected.Value == true).ToArray())
                                   .ToReadOnlyReactivePropertySlim(Array.Empty<LayerTreeViewItemBase>());

            SelectedLayers.Subscribe(x =>
            {
                Trace.WriteLine($"SelectedLayers changed {string.Join(", ", x.Select(x => x.ToString()))}");
            })
            .AddTo(_CompositeDisposable);

            Layers.ObserveAddChanged()
                  .Subscribe(x =>
            {
                RootLayer.Value.Children = new ReactiveCollection<LayerTreeViewItemBase>(Layers.Cast<LayerTreeViewItemBase>().ToObservable());
                x.SetParentToChildren(RootLayer.Value);
            })
            .AddTo(_CompositeDisposable);

            Width = width;
            Height = height;
        }

        private void MoveSelectedItems(int horizontalDiff, int verticalDiff)
        {
            MainWindowVM.Recorder.BeginRecode();
            SelectedItems.Value.OfType<DesignerItemViewModelBase>().ToList().ForEach(x =>
            {
                MainWindowVM.Recorder.Current.ExecuteSetProperty(x, "Left.Value", x.Left.Value + horizontalDiff);
                MainWindowVM.Recorder.Current.ExecuteSetProperty(x, "Top.Value", x.Top.Value + verticalDiff);
            });
            SelectedItems.Value.OfType<SnapPointViewModel>().ToList().ForEach(x =>
            {
                MainWindowVM.Recorder.Current.ExecuteSetProperty(x, "Left.Value", x.Left.Value + horizontalDiff);
                MainWindowVM.Recorder.Current.ExecuteSetProperty(x, "Top.Value", x.Top.Value + verticalDiff);
            });
            MainWindowVM.Recorder.EndRecode();
        }

        public void Initialize()
        {
            MainWindowVM.Recorder.BeginRecode();

            InitialSetting(MainWindowVM, true, true);
            
            MainWindowVM.Recorder.EndRecode();
            
            MainWindowVM.Controller.Flush();
        }

        public LayerTreeViewItemBase GetLayerTreeViewItemBase(SelectableDesignerItemViewModelBase item)
        {
            return Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                         .Where(x => x is LayerItem)
                         .First(x => (x as LayerItem).Item.Value == item);
        }

        private Point GetCenter(SnapPoint snapPoint)
        {
            var designerCanvas = App.Current.MainWindow.GetChildOfType<DesignerCanvas>();
            var leftTop = snapPoint.TransformToAncestor(designerCanvas).Transform(new Point(0, 0));
            switch (snapPoint.Tag)
            {
                case "左上":
                    return new Point(leftTop.X + snapPoint.Width - 1, leftTop.Y + snapPoint.Height - 1);
                case "右上":
                    return new Point(leftTop.X, leftTop.Y + snapPoint.Height - 1);
                case "左下":
                    return new Point(leftTop.X + snapPoint.Width - 1, leftTop.Y);
                case "右下":
                    return new Point(leftTop.X, leftTop.Y);
                case "左":
                case "上":
                case "右":
                case "下":
                    return new Point(leftTop.X, leftTop.Y);
                case "始点":
                case "終点":
                case "制御点":
                case "独立点":
                    return new Point(leftTop.X + snapPoint.Width / 2, leftTop.Y + snapPoint.Height / 2);
                default:
                    throw new Exception("ResizeThumb.Tag doesn't set");
            }
        }

        private void InitialSetting(MainWindowViewModel mainwindowViewModel, bool addingLayer = false, bool initCanvasBackground = false)
        {
            mainwindowViewModel.Recorder.Current.ExecuteAdd(EdgeColors, Colors.Black);
            mainwindowViewModel.Recorder.Current.ExecuteAdd(FillColors, Colors.White);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "EdgeThickness.Value", 1.0);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "CanvasBorderThickness", 0.0);
            if (initCanvasBackground)
            {
                mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "CanvasBackground.Value", Colors.White);
            }
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value", new BackgroundViewModel());
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.ZIndex.Value", -1);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.FillColor.Value", CanvasBackground.Value);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.Left.Value", 0d);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.Top.Value", 0d);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.Width.Value", (double)Width);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.Height.Value", (double)Height);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.Owner", this);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.EdgeColor.Value", Colors.Black);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.EdgeThickness.Value", 1d);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.EnableForSelection.Value", false);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "BackgroundItem.Value.IsVisible.Value", true);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "EnablePointSnap.Value", true);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "LayerCount", 1);
            mainwindowViewModel.Recorder.Current.ExecuteSetProperty(this, "LayerItemCount", 1);
            RootLayer.Dispose();
            RootLayer = new ReactivePropertySlim<LayerTreeViewItemBase>(new LayerTreeViewItemBase());
            Layers.ToClearOperation().ExecuteTo(mainwindowViewModel.Recorder.Current);
            if (addingLayer)
            {
                var layer = new Layer();
                layer.IsVisible.Value = true;
                layer.IsSelected.Value = true;
                layer.Name.Value = Name.GetNewLayerName(this);
                Random rand = new Random();
                layer.Color.Value = Randomizer.RandomColor(rand);
                mainwindowViewModel.Recorder.Current.ExecuteAdd(Layers, layer);
            }
        }

        private void ExecuteRedoCommand()
        {
            MainWindowVM.Controller.Redo();
            RedoCommand.RaiseCanExecuteChanged();
        }

        public bool CanExecuteRedo()
        {
            return MainWindowVM.Controller.CanRedo;
        }

        private void ExecuteUndoCommand()
        {
            MainWindowVM.Controller.Undo();
            UndoCommand.RaiseCanExecuteChanged();
            RedoCommand.RaiseCanExecuteChanged();
        }

        public bool CanExecuteUndo()
        {
            return MainWindowVM.Controller.CanUndo;
        }

        private void ReleaseMiddleButton(MouseEventArgs args)
        {
            if (args.MiddleButton == MouseButtonState.Released)
            {
                _MiddleButtonIsPressed = false;
                var diagramControl = App.Current.MainWindow.GetChildOfType<DiagramControl>();
                diagramControl.Cursor = Cursors.Arrow;
            }
        }

        public DiagramViewModel(MainWindowViewModel MainWindowVM, IDialogService dlgService, int width, int height)
            : this(MainWindowVM, width, height)
        {
            this.dlgService = dlgService;

            Mediator.Instance.Register(this);
        }

        public void DeselectAll()
        {
            foreach (var layerItem in Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                            .Where(x => x is LayerItem))
            {
                (layerItem as LayerItem).Item.Value.IsSelected.Value = false;
                (layerItem as LayerItem).IsSelected.Value = false;
            }
        }

        private void ExecuteAddItemCommand(object parameter)
        {
            if (parameter is SelectableDesignerItemViewModelBase item)
            {
                var targetLayer = SelectedLayers.Value.First();
                var newZIndex = targetLayer.GetNewZIndex(Layers.TakeWhile(x => x != targetLayer));
                Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                      .Where(x => x != targetLayer)
                      .ToList()
                      .ForEach(x => x.PushZIndex(MainWindowVM.Recorder, newZIndex));
                item.ZIndex.Value = newZIndex;
                item.Owner = this;
                Add(item);
            }
        }

        private void ExecuteRemoveItemCommand(object parameter)
        {
            if (parameter is SelectableDesignerItemViewModelBase)
            {
                SelectableDesignerItemViewModelBase item = (SelectableDesignerItemViewModelBase)parameter;
                if (item is SnapPointViewModel snapPoint)
                {
                    item = snapPoint.Parent.Value;
                }
                RemoveGroupMembers(item);
                Remove(item);
                item.Dispose();
                UpdateZIndex();
            }
        }

        private void UpdateZIndex()
        {
            var items = (from item in Layers.SelectMany(x => x.Children)
                         orderby (item as LayerItem).Item.Value.ZIndex.Value ascending
                         select item).ToList();

            for (int i = 0; i < items.Count; ++i)
            {
                (items.ElementAt(i) as LayerItem).Item.Value.ZIndex.Value = i;
            }
        }

        private void RemoveGroupMembers(SelectableDesignerItemViewModelBase item)
        {
        }

        private void ExecuteClearSelectedItemsCommand(object parameter)
        {
            foreach (LayerItem layerItem in Layers.SelectMany(x => x.Children))
            {
                layerItem.Item.Value.IsSelected.Value = false;
            }
        }

        private void Add(SelectableDesignerItemViewModelBase item)
        {
            SelectedLayers.Value.First().AddItem(MainWindowVM, this, item);
        }

        private void Remove(SelectableDesignerItemViewModelBase item)
        {
            Layers.ToList().ForEach(x => x.RemoveItem(MainWindowVM, item));
        }

        private void ExecuteSelectAllCommand()
        {
            Layers.SelectMany(x => x.Children).ToList().ForEach(x => (x as LayerItem).Item.Value.IsSelected.Value = true);
        }

        private IEnumerable<SelectableDesignerItemViewModelBase> GetGroupMembers(SelectableDesignerItemViewModelBase item)
        {
            var list = new List<SelectableDesignerItemViewModelBase>();
            list.Add(item);
            var children = Layers.SelectMany(x => x.Children)
                                 .Where(x => (x as LayerItem).Item.Value.ParentID == item.ID)
                                 .Select(x => (x as LayerItem).Item.Value);
            list.AddRange(children);
            return list;
        }

        public static Rect GetBoundingRectangle(IEnumerable<SelectableDesignerItemViewModelBase> items)
        {
            double x1 = Double.MaxValue;
            double y1 = Double.MaxValue;
            double x2 = Double.MinValue;
            double y2 = Double.MinValue;

            foreach (var item in items)
            {
                if (item is DesignerItemViewModelBase designerItem)
                {
                    var centerPoint = designerItem.CenterPoint.Value;
                    var angleInDegrees = designerItem.RotationAngle.Value;

                    var p0 = new Point(designerItem.Left.Value + designerItem.Width.Value, designerItem.Top.Value + designerItem.Height.Value / 2);
                    var p1 = new Point(designerItem.Left.Value, designerItem.Top.Value);
                    var p2 = new Point(designerItem.Left.Value + designerItem.Width.Value, designerItem.Top.Value);
                    var p3 = new Point(designerItem.Left.Value + designerItem.Width.Value, designerItem.Top.Value + designerItem.Height.Value);
                    var p4 = new Point(designerItem.Left.Value, designerItem.Top.Value + designerItem.Height.Value);

                    var vector_p0_center = p0 - centerPoint;
                    var vector_p1_center = p1 - centerPoint;
                    var vector_p2_center = p2 - centerPoint;
                    var vector_p3_center = p3 - centerPoint;
                    var vector_p4_center = p4 - centerPoint;

                    UpdateBoundary(ref x1, ref y1, ref x2, ref y2, centerPoint, angleInDegrees + Vector.AngleBetween(vector_p0_center, vector_p1_center), p1);
                    UpdateBoundary(ref x1, ref y1, ref x2, ref y2, centerPoint, angleInDegrees + Vector.AngleBetween(vector_p0_center, vector_p2_center), p2);
                    UpdateBoundary(ref x1, ref y1, ref x2, ref y2, centerPoint, angleInDegrees + Vector.AngleBetween(vector_p0_center, vector_p3_center), p3);
                    UpdateBoundary(ref x1, ref y1, ref x2, ref y2, centerPoint, angleInDegrees + Vector.AngleBetween(vector_p0_center, vector_p4_center), p4);
                }
                else if (item is ConnectorBaseViewModel connector)
                {
                    x1 = Math.Min(Math.Min(connector.Points[0].X, connector.Points[1].X), x1);
                    y1 = Math.Min(Math.Min(connector.Points[0].Y, connector.Points[1].Y), y1);

                    x2 = Math.Max(Math.Max(connector.Points[0].X, connector.Points[1].X), x2);
                    y2 = Math.Max(Math.Max(connector.Points[0].Y, connector.Points[1].Y), y2);
                }
            }

            return new Rect(new Point(x1, y1), new Point(x2, y2));
        }

        private static void UpdateBoundary(ref double x1, ref double y1, ref double x2, ref double y2, Point centerPoint, double angleInDegrees, Point point)
        {
            var rad = angleInDegrees * Math.PI / 180;

            var t = RotatePoint(centerPoint, point, rad);

            x1 = Math.Min(t.Item1, x1);
            y1 = Math.Min(t.Item2, y1);
            x2 = Math.Max(t.Item1, x2);
            y2 = Math.Max(t.Item2, y2);
        }

        private static Tuple<double, double> RotatePoint(Point center, Point point, double rad)
        {
            var z1 = point.X - center.X;
            var z2 = point.Y - center.Y;
            var x = center.X + Math.Sqrt(Math.Pow(z1, 2) + Math.Pow(z2, 2)) * Math.Cos(rad);
            var y = center.Y + Math.Sqrt(Math.Pow(z1, 2) + Math.Pow(z2, 2)) * Math.Sin(rad);

            return new Tuple<double, double>(x, y);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    Layers.Dispose();
                    AllItems.Dispose();
                    SelectedItems.Dispose();
                    EdgeThickness.Dispose();
                    EnableMiniMap.Dispose();
                    FileName.Dispose();
                    CanvasBackground.Dispose();
                    EnablePointSnap.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            // このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }
    }
}

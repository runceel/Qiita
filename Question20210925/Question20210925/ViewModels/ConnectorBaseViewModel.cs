using boilersGraphics.Controls;
using boilersGraphics.Helpers;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Media;

namespace boilersGraphics.ViewModels
{
    public abstract class ConnectorBaseViewModel : SelectableDesignerItemViewModelBase, ICloneable
    {
        private ObservableCollection<Point> _Points;

        public ConnectorBaseViewModel(int id, IDiagramViewModel parent) : base(id, parent)
        {
            Init();
        }

        public ConnectorBaseViewModel()
        {
            Init();
        }

        public ReactiveProperty<Point> LeftTop { get; set; }

        public ReadOnlyReactivePropertySlim<double> Width { get; set; }

        public ReadOnlyReactivePropertySlim<double> Height { get; set; }

        public ObservableCollection<Point> Points
        {
            get { return _Points; }
            set { SetProperty(ref _Points, value); }
        }
        public ReadOnlyReactivePropertySlim<SnapPointViewModel> SnapPoint0VM { get; protected set; }
        public ReadOnlyReactivePropertySlim<SnapPointViewModel> SnapPoint1VM { get; protected set; }

        private void Init()
        {
            _Points = new ObservableCollection<Point>();
            InitPathFinder();
            LeftTop = Points.ObserveProperty(x => x.Count)
                            .Where(x => x > 0)
                            .Select(_ => new Point(Points.Min(x => x.X), Points.Min(x => x.Y)))
                            .ToReactiveProperty();
            Width = Points.ObserveProperty(x => x.Count)
                          .Where(x => x > 0)
                          .Select(_ => Points.Max(x => x.X) - Points.Min(x => x.X))
                          .ToReadOnlyReactivePropertySlim();
            Height = Points.ObserveProperty(x => x.Count)
                          .Where(x => x > 0)
                          .Select(_ => Points.Max(x => x.Y) - Points.Min(x => x.Y))
                          .ToReadOnlyReactivePropertySlim();
        }

        protected virtual void InitPathFinder() { }

        #region IObserver<TransformNotification>

        public void OnNext(TransformNotification value)
        {
        }

        #endregion //IObserver<TransformNotification>
    }
}

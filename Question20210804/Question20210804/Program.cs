using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;

namespace Question20210804
{
    class Program
    {
        static void Main(string[] args)
        {
            var diagramViewModel = new DiagramViewModel();

            var layer1 = new Layer();
            layer1.Name.Value = "レイヤー１";
            Console.WriteLine("Add layer1");
            diagramViewModel.Layers.Add(layer1);

            var layer2 = new Layer();
            layer2.Name.Value = "レイヤー２";
            Console.WriteLine("Add layer2");
            diagramViewModel.Layers.Add(layer2);

            Console.WriteLine("set IsSelect={true, false}");
            diagramViewModel.Layers[0].IsSelected.Value = true;
            diagramViewModel.Layers[1].IsSelected.Value = false;
            Console.WriteLine($"{diagramViewModel.SelectedLayers.Value.Count()} items");
            Console.WriteLine(string.Join(", ", diagramViewModel.SelectedLayers.Value.Select(x => $"{x.Name.Value.ToString()}[{x.IsSelected.Value}]")));

            Console.WriteLine("Add item1 to layer1");
            var item1 = new SelectableDesignerItemViewModelBase();
            diagramViewModel.SelectedLayers.Value.First().AddItem(item1);
            Console.WriteLine(string.Join(", ", diagramViewModel.AllItems.Value.Select(x => x)));

            Console.WriteLine("Add item2 to layer1");
            var item2 = new SelectableDesignerItemViewModelBase();
            diagramViewModel.SelectedLayers.Value.First().AddItem(item2);
            Console.WriteLine(string.Join(", ", diagramViewModel.AllItems.Value.Select(x => x)));

            //Console.WriteLine("set IsSelect={false, true}");
            //diagramViewModel.Layers[0].IsSelected.Value = false;
            //diagramViewModel.Layers[1].IsSelected.Value = true;
            //Console.WriteLine($"{diagramViewModel.SelectedLayers.Value.Count()} items");
            //Console.WriteLine(string.Join(", ", diagramViewModel.SelectedLayers.Value.Select(x => $"{x.Name.Value.ToString()}[{x.IsSelected.Value}]")));

            //Console.WriteLine("set IsSelect={true, false}");
            //diagramViewModel.Layers[0].IsSelected.Value = true;
            //diagramViewModel.Layers[1].IsSelected.Value = false;
            //Console.WriteLine($"{diagramViewModel.SelectedLayers.Value.Count()} items");
            //Console.WriteLine(string.Join(", ", diagramViewModel.SelectedLayers.Value.Select(x => $"{x.Name.Value.ToString()}[{x.IsSelected.Value}]")));

            //Console.WriteLine("add LayerItem");
            //var item = new SelectableDesignerItemViewModelBase();
            //var layerItem = new LayerItem(item, diagramViewModel.Layers[0]);
            //diagramViewModel.Layers[0].Items.Add(layerItem);
            //Console.WriteLine("switch IsSelected to true");
            //item.IsSelected.Value = true;
            //Console.WriteLine("switch IsSelected to false");
            //item.IsSelected.Value = false;
        }
    }

    class DiagramViewModel : BindableBase
    {
        public ReactivePropertySlim<LayerTreeViewItemBase> RootLayer { get; set; } = new ReactivePropertySlim<LayerTreeViewItemBase>(new LayerTreeViewItemBase());

        public ReactiveCollection<LayerTreeViewItemBase> Layers { get; }

        public ReadOnlyReactivePropertySlim<LayerTreeViewItemBase[]> SelectedLayers { get; }

        public ReadOnlyReactivePropertySlim<SelectableDesignerItemViewModelBase[]> AllItems { get; }

        public ReadOnlyReactivePropertySlim<SelectableDesignerItemViewModelBase[]> SelectedItems { get; }

        public DiagramViewModel()
        {
            Layers = RootLayer.Value.Children.CollectionChangedAsObservable()
                           .Select(_ => RootLayer.Value.LayerChangedAsObservable())
                           .Switch()
                           .SelectMany(_ => RootLayer.Value.Children)
                           .ToReactiveCollection();
            AllItems = Layers.CollectionChangedAsObservable()
                             .Select(_ => Layers.Select(x => x.LayerItemsChangedAsObservable()).Merge())
                             .Switch()
                             .Select(_ => Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                                .Where(x => x.GetType() == typeof(LayerItem))
                                                .Select(y => (y as LayerItem).Item.Value)
                                                .ToArray())
                             .ToReadOnlyReactivePropertySlim(Array.Empty<SelectableDesignerItemViewModelBase>());
            SelectedItems = Layers.CollectionChangedAsObservable()
                                  .Select(_ => Layers.Select(x => x.SelectedLayerItemsChangedAsObservable()).Merge())
                                  .Switch()
                                  .Select(_ => Layers.SelectRecursive<LayerTreeViewItemBase, LayerTreeViewItemBase>(x => x.Children)
                                                     .Where(x => x.GetType() == typeof(LayerItem))
                                                     .Select(y => (y as LayerItem).Item.Value)
                                                     .Where(z => z.IsSelected.Value == true)
                                                     .ToArray())
                                  .ToReadOnlyReactivePropertySlim(Array.Empty<SelectableDesignerItemViewModelBase>());
            SelectedLayers = Layers.ObserveElementObservableProperty(x => x.IsSelected)
                                   .Select(_ => Layers.Where(x => x.IsSelected.Value == true).ToArray())
                                   .ToReadOnlyReactivePropertySlim(Array.Empty<LayerTreeViewItemBase>());
            Layers.ObserveAddChanged()
                  .Subscribe(x =>
                  {
                      RootLayer.Value.Children = new ReactiveCollection<LayerTreeViewItemBase>(Layers.Cast<LayerTreeViewItemBase>().ToObservable());
                      x.SetParentToChildren(RootLayer.Value);
                  });
        }
    }

    public class LayerTreeViewItemBase : BindableBase
    {
        public ReactivePropertySlim<LayerTreeViewItemBase> Parent { get; } = new ReactivePropertySlim<LayerTreeViewItemBase>();

        public ReactiveProperty<bool> IsSelected { get; set; } = new ReactiveProperty<bool>();

        public ReactivePropertySlim<string> Name { get; } = new ReactivePropertySlim<string>();

        public ReactiveCollection<LayerTreeViewItemBase> Children { get; set; } = new ReactiveCollection<LayerTreeViewItemBase>();

        public void AddItem(SelectableDesignerItemViewModelBase item)
        {
            var layerItem = new LayerItem(item, this);
            layerItem.Parent.Value = this;
            Children.Add(layerItem);
        }

        public IObservable<Unit> LayerChangedAsObservable()
        {
            return this.Children.CollectionChangedAsObservable().Where(x => x.Action == NotifyCollectionChangedAction.Remove || x.Action == NotifyCollectionChangedAction.Reset).ToUnit();
        }

        public IObservable<Unit> LayerItemsChangedAsObservable()
        {
            return Children.ObserveElementObservableProperty(x => (x as LayerItem).Item)
                        .ToUnit()
                        .Merge(Children.CollectionChangedAsObservable().Where(x => x.Action == NotifyCollectionChangedAction.Remove || x.Action == NotifyCollectionChangedAction.Reset).ToUnit());
        }

        public IObservable<Unit> SelectedLayerItemsChangedAsObservable()
        {
            return Children.ObserveElementObservableProperty(x => (x as LayerItem).Item.Value.IsSelected)
                        .ToUnit()
                        .Merge(Children.CollectionChangedAsObservable().Where(x => x.Action == NotifyCollectionChangedAction.Remove || x.Action == NotifyCollectionChangedAction.Reset).ToUnit());
        }

        public void SetParentToChildren(LayerTreeViewItemBase parent = null)
        {
            Parent.Value = parent;

            if (Children == null)
                return;
            foreach (var child in Children)
            {
                child.SetParentToChildren(this);
            }
        }
    }

    public class Layer : LayerTreeViewItemBase, IObservable<LayerObservable>
    {

        public Layer()
        {

        }

        private List<IObserver<LayerObservable>> _observers = new List<IObserver<LayerObservable>>();

        public IDisposable Subscribe(IObserver<LayerObservable> observer)
        {
            _observers.Add(observer);
            observer.OnNext(new LayerObservable());
            return new LayerDisposable(this, observer);
        }

        public class LayerDisposable : IDisposable
        {
            private Layer layer;
            private IObserver<LayerObservable> observer;

            public LayerDisposable(Layer layer, IObserver<LayerObservable> observer)
            {
                this.layer = layer;
                this.observer = observer;
            }

            public void Dispose()
            {
                layer._observers.Remove(observer);
            }
        }
    }

    public class LayerObservable : BindableBase
    {
    }

    public class LayerItem : LayerTreeViewItemBase
    {
        public ReactivePropertySlim<SelectableDesignerItemViewModelBase> Item { get; } = new ReactivePropertySlim<SelectableDesignerItemViewModelBase>();

        public LayerItem(SelectableDesignerItemViewModelBase item, LayerTreeViewItemBase owner)
        {
            Item.Value = item;
            Parent.Value = owner;
            Init();
        }

        private void Init()
        {
            IsSelected = this.ObserveProperty(x => x.Item.Value.IsSelected)
                             .Select(x => x.Value)
                             .ToReactiveProperty();
        }
    }

    public class SelectableDesignerItemViewModelBase : BindableBase, IObservable<SelectableDesignerItemViewModelBaseObservable>
    {
        public ReactivePropertySlim<bool> IsSelected { get; } = new ReactivePropertySlim<bool>();

        private List<IObserver<SelectableDesignerItemViewModelBaseObservable>> _observers = new List<IObserver<SelectableDesignerItemViewModelBaseObservable>>();

        public SelectableDesignerItemViewModelBase()
        {
            IsSelected.Subscribe();
        }

        public IDisposable Subscribe(IObserver<SelectableDesignerItemViewModelBaseObservable> observer)
        {
            _observers.Add(observer);
            observer.OnNext(new SelectableDesignerItemViewModelBaseObservable(this));
            return new SelectableDesignerItemViewModelBaseDisposable(this, observer);
        }

        public class SelectableDesignerItemViewModelBaseDisposable : IDisposable
        {
            private SelectableDesignerItemViewModelBase item;
            private IObserver<SelectableDesignerItemViewModelBaseObservable> observer;

            public SelectableDesignerItemViewModelBaseDisposable(SelectableDesignerItemViewModelBase item, IObserver<SelectableDesignerItemViewModelBaseObservable> observer)
            {
                this.item = item;
                this.observer = observer;
            }

            public void Dispose()
            {
                item._observers.Remove(observer);
            }
        }
    }

    public class SelectableDesignerItemViewModelBaseObservable : BindableBase
    {
        public SelectableDesignerItemViewModelBase Owner { get; }
        public SelectableDesignerItemViewModelBaseObservable(SelectableDesignerItemViewModelBase owner)
        {
            Owner = owner;
        }
    }

    static class Extensions
    {
        //https://stackoverflow.com/questions/41608665/linq-recursive-parent-child
        public static IEnumerable<T2> SelectRecursive<T1, T2>(this IEnumerable<T1> source, Func<T2, IEnumerable<T2>> selector) where T1 : class where T2 : class
        {
            foreach (var parent in source)
            {
                yield return parent as T2;

                var children = selector(parent as T2);
                foreach (var child in SelectRecursive(children, selector))
                    yield return child;
            }
        }
    }
}
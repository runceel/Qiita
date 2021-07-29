using Prism.Mvvm;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace Comment20210729
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
            Console.WriteLine($"{diagramViewModel.SelectedLayers.Count} items");
            Console.WriteLine(string.Join(", ", diagramViewModel.SelectedLayers.Select(x => $"{x.Name.Value.ToString()}[{x.IsSelected.Value}]")));

            Console.WriteLine("set IsSelect={false, true}");
            diagramViewModel.Layers[0].IsSelected.Value = false;
            diagramViewModel.Layers[1].IsSelected.Value = true;
            Console.WriteLine($"{diagramViewModel.SelectedLayers.Count} items");
            Console.WriteLine(string.Join(", ", diagramViewModel.SelectedLayers.Select(x => $"{x.Name.Value.ToString()}[{x.IsSelected.Value}]")));
        }
    }

    class DiagramViewModel : BindableBase
    {

        public ReactiveCollection<Layer> Layers { get; } = new ReactiveCollection<Layer>();

        public ReadOnlyReactiveCollection<Layer> SelectedLayers { get; }

        public DiagramViewModel()
        {
            //SelectedLayers = Layers.ObserveElementProperty(x => x.IsSelected)
            //                       .Select(x => x.Instance)
            //                       .Where(x => x.IsSelected.Value)
            //                       .ToReadOnlyReactiveCollection(); //not working

            SelectedLayers = Layers.ObserveElementObservableProperty(x => x.IsSelected)
                                   .Select(x => x.Instance)
                                   .Where(x => x.IsSelected.Value)
                                   .ToReadOnlyReactiveCollection();
        }
    }

    class Layer : BindableBase, IObservable<LayerObservable>
    {

        public ReactivePropertySlim<bool> IsSelected { get; } = new ReactivePropertySlim<bool>();

        public ReactivePropertySlim<string> Name { get; } = new ReactivePropertySlim<string>();

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
}

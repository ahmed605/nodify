﻿using System.Linq;

namespace Nodify.Calculator
{
    public class CalculatorViewModel : ObservableObject
    {
        public CalculatorViewModel()
        {
            CreateConnectionCommand = new DelegateCommand<(object Source, object Target)>(target => CreateConnection((ConnectorViewModel)target.Source, (ConnectorViewModel)target.Target), target => CanCreateConnection((ConnectorViewModel)target.Source, target.Target as ConnectorViewModel));
            CreateOperationCommand = new DelegateCommand<CreateOperationInfoViewModel>(CreateOperation);
            DisconnectConnectorCommand = new DelegateCommand<ConnectorViewModel>(DisconnectConnector);
            DeleteSelectionCommand = new DelegateCommand(DeleteSelection);
            GroupSelectionCommand = new DelegateCommand(GroupSelectedOperations, () => SelectedOperations.Count > 0);

            Connections.WhenAdded(c =>
            {
                c.Input.IsConnected = true;
                c.Output.IsConnected = true;

                c.Input.Value = c.Output.Value;

                c.Output.ValueObservers.Add(c.Input);
            })
            .WhenRemoved(c =>
            {
                var ic = Connections.Count(con => con.Input == c.Input || con.Output == c.Input);
                var oc = Connections.Count(con => con.Input == c.Output || con.Output == c.Output);

                if (ic == 0)
                {
                    c.Input.IsConnected = false;
                }

                if (oc == 0)
                {
                    c.Output.IsConnected = false;
                }

                c.Output.ValueObservers.Remove(c.Input);
            });

            Operations.WhenAdded(x =>
            {
                x.Input.WhenRemoved(RemoveConnection);

                if (x is CalculatorInputOperationViewModel ci)
                {
                    ci.Output.WhenRemoved(RemoveConnection);
                }

                void RemoveConnection(ConnectorViewModel i)
                {
                    var c = Connections.Where(con => con.Input == i || con.Output == i).ToArray();
                    c.ForEach(con => Connections.Remove(con));
                }
            })
            .WhenRemoved(x =>
            {
                foreach (var input in x.Input)
                {
                    DisconnectConnector(input);
                }

                if (x.Output != null)
                {
                    DisconnectConnector(x.Output);
                }
            });

            UpdateAvailableOperations();
        }

        private NodifyObservableCollection<OperationViewModel> _operations = new NodifyObservableCollection<OperationViewModel>();
        public NodifyObservableCollection<OperationViewModel> Operations
        {
            get => _operations;
            set => SetProperty(ref _operations, value);
        }

        private NodifyObservableCollection<OperationViewModel> _selectedOperations = new NodifyObservableCollection<OperationViewModel>();
        public NodifyObservableCollection<OperationViewModel> SelectedOperations
        {
            get => _selectedOperations;
            set => SetProperty(ref _selectedOperations, value);
        }

        public NodifyObservableCollection<ConnectionViewModel> Connections { get; } = new NodifyObservableCollection<ConnectionViewModel>();

        public NodifyObservableCollection<OperationInfoViewModel> AvailableOperations { get; } = new NodifyObservableCollection<OperationInfoViewModel>();

        public INodifyCommand CreateConnectionCommand { get; }
        public INodifyCommand CreateOperationCommand { get; }
        public INodifyCommand DisconnectConnectorCommand { get; }
        public INodifyCommand DeleteSelectionCommand { get; }
        public INodifyCommand GroupSelectionCommand { get; }

        private void UpdateAvailableOperations()
        {
            AvailableOperations.Add(new OperationInfoViewModel
            {
                Type = OperationType.Calculator,
                Title = "Calculator"
            });
            AvailableOperations.Add(new OperationInfoViewModel
            {
                Type = OperationType.Expression,
                Title = "Custom",
            });
            var operations = OperationFactory.GetOperationsInfo(typeof(OperationsContainer));
            AvailableOperations.AddRange(operations);
        }

        private void DisconnectConnector(ConnectorViewModel connector)
        {
            var connections = Connections.Where(c => c.Input == connector || c.Output == connector).ToList();
            connections.ForEach(c => Connections.Remove(c));
        }

        private bool CanCreateConnection(ConnectorViewModel source, ConnectorViewModel? target)
            => target != null && source != target && source.Operation != target.Operation && source.IsInput != target.IsInput;

        private void CreateConnection(ConnectorViewModel source, ConnectorViewModel target)
        {
            var input = source.IsInput ? source : target;
            var output = target.IsInput ? source : target;

            DisconnectConnector(input);

            Connections.Add(new ConnectionViewModel
            {
                Input = input,
                Output = output
            });
        }

        private void CreateOperation(CreateOperationInfoViewModel arg)
        {
            var op = OperationFactory.GetOperation(arg.Info);
            op.Location = arg.Location;

            Operations.Add(op);
        }

        private void DeleteSelection()
        {
            var selected = SelectedOperations.ToList();
            selected.ForEach(o => Operations.Remove(o));
        }

        private void GroupSelectedOperations()
        {
            var selected = SelectedOperations.ToList();
            var bounding = selected.GetBoundingBox(50);

            Operations.Add(new OperationGroupViewModel
            {
                Title = "Operations",
                Location = bounding.Location,
                Width = bounding.Width,
                Height = bounding.Height
            });
        }
    }
}


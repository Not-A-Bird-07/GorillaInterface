using ComputerInterface.Extensions;
using ComputerInterface.Interfaces;
using ComputerInterface.ViewLib;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ComputerInterface.Views.GameSettings
{
    internal class QueueView : ComputerView
    {
        private readonly List<IQueueInfo> _queues;
        private readonly UISelectionHandler _selectionHandler;

        public QueueView(List<IQueueInfo> queues)
        {
            _queues = queues;

            _selectionHandler = new UISelectionHandler(EKeyboardKey.Up, EKeyboardKey.Down);
            _selectionHandler.ConfigureSelectionIndicator($"<color=#{PrimaryColor}> ></color> ", "", "   ", "");
            _selectionHandler.MaxIdx = queues.Count;
        }

        public override void OnShow(object[] args)
        {
            base.OnShow(args);

            string prefsQueue = BaseGameInterface.GetQueue();

            //if the pref does not equal the builtin game ones it breaks the view so this fixes it

            for (int i = 0; i < _queues.Count; i++)
            {
                if (_queues[i].DisplayName != prefsQueue)
                {
                    prefsQueue = "Default";
                    BaseGameInterface.SetQueue(_queues[0]);
                }
            }

            _selectionHandler.CurrentSelectionIndex = _queues.IndexOf(_queues.First(queue => queue.DisplayName == prefsQueue));
            BaseGameInterface.SetQueue(_queues[_selectionHandler.CurrentSelectionIndex]);

            Redraw();
        }

        public void Redraw()
        {
            StringBuilder str = new();

            str.BeginCenter().Repeat("=", SCREEN_WIDTH).AppendLine();
            str.Append("Queue Tab").AppendLine();
            str.Repeat("=", SCREEN_WIDTH).EndAlign().AppendLines(2);

            for (int i = 0; i < _queues.Count; i++)
            {
                str.Append(_selectionHandler.GetIndicatedText(i, _queues[i].DisplayName));
                str.AppendLine();
            }

            str.AppendLine().BeginColor("ffffff50").Append("* ").EndColor().Append(_queues[_selectionHandler.CurrentSelectionIndex].Description);

            SetText(str);
        }

        public override void OnKeyPressed(EKeyboardKey key)
        {
            switch (key)
            {
                case EKeyboardKey.Back:
                    ShowView<GameSettingsView>();
                    break;
                default:
                    if (_selectionHandler.HandleKeypress(key))
                    {
                        BaseGameInterface.SetQueue(_queues[_selectionHandler.CurrentSelectionIndex]);
                        Redraw();
                        return;
                    }
                    break;
            }
        }
    }
}

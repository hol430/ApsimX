﻿using APSIM.Server.Commands;
using System;
using System.Collections.Generic;

namespace APSIM.Server.IO
{
    class CommandListConnectionManager : IConnectionManager
    {
        private readonly Queue<ICommand> commands = new Queue<ICommand>();
        private readonly Action<ICommand, Exception> completedCallBack;

        public CommandListConnectionManager(IEnumerable<ICommand> commandList, Action<ICommand, Exception> commandCompleteCallBack)
        {
            foreach (var command in commandList)
                commands.Enqueue(command);
            completedCallBack = commandCompleteCallBack;
        }
        public void Disconnect()
        {
        }

        public void Dispose()
        {
        }

        public void OnCommandFinished(ICommand command, Exception error = null)
        {
            if (completedCallBack != null)
                completedCallBack(command, error);
        }

        public void SendCommand(ICommand command)
        {
            throw new NotImplementedException();
        }

        public ICommand WaitForCommand()
        {
            if (commands.TryDequeue(out ICommand command))
                return command;
            return null;
        }

        public void WaitForConnection()
        {
        }
    }
}

﻿namespace NitroComposer.SequenceCommands {
    public class TieCommand : BaseSequenceCommand {
        public bool Tie;

        public TieCommand(bool tie) {
            Tie = tie;
        }
    }
}
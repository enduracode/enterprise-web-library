using System;

namespace RedStapler.StandardLibrary {
    public class UnhandledEnumException: UnhandledSwitchCaseException {
        public UnhandledEnumException( Enum e ): base( e.GetType().Name, e ) {}
    }
}
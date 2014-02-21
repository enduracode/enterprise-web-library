using System;

namespace RedStapler.StandardLibrary {
    public class UnhandledSwitchCaseException: Exception {
        private readonly string typeName;
        private readonly object value;

        public UnhandledSwitchCaseException( string typeName, object value ) {
            this.typeName = typeName;
            this.value = value;
        }

        public override string Message { get { return "Unexpected {0}: '{1}'".FormatWith( typeName, value ); } }
    }
}
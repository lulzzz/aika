using System;
using System.Collections.Generic;
using System.Text;
using Elasticsearch.Net;

namespace Aika.Elasticsearch {
    public class ElasticException : ApplicationException {

        public ServerError ElasticError { get; }

        public ElasticException(string message, ServerError serverError) : base(message) {
            ElasticError = serverError;
        }


        public ElasticException(string message, Exception innerException, ServerError serverError) : base(message, innerException) {
            ElasticError = serverError;
        }

    }
}

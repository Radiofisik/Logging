using Dtos;
using System;
using System.Collections.Generic;
using System.Text;

namespace Services.Abstractions
{
    public interface IExampleService: IService
    {
        OutputDto DoSomething(InputDto input);
    }
}

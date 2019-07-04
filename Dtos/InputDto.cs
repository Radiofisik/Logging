using System;

namespace Dtos
{
    public class InputDto
    {
        public Guid Id { get; set; } = Guid.NewGuid();

        public string SomeParam { get; set; }
    }
}

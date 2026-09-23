using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DesignPattern
{
    public interface IOriginator
    {
        IMemento Save();
    }
}
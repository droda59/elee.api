using System;

namespace E133.Business.Bases
{
    public sealed class RicardoBase : IBase
    {
        private readonly Uri _domain;

        public RicardoBase()
        {
            this._domain = new Uri("https://www.ricardocuisine.com/");
        }

        public Uri Domain => this._domain;
    }
}
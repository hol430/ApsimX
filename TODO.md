When replacing a model via an experiment factor, we need to:

    Reconnect all event handlers which subscribe to events published by the old model
    Disconnect all event handlers in the old model
    Disconnect all links in the old model
    Reconnect all links in all models in the simulation which point to the old model or any of its descendants

We also need to account for the soil having already been standardised when factor replacements are applied to the simulation. Currently (ie in the current release), the factor replacements are applied before the soil has been standardised, which allows them to, for example, modify [Soil].Initial water.SW which doesn't exist after the soil has been standardised.


﻿namespace Coevery.Recipes.Services {
    public interface IRecipeStepExecutor : IDependency {
        bool ExecuteNextStep(string executionId);
    }
}

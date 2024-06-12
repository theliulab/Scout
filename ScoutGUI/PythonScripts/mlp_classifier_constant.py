from sklearn.neural_network import MLPClassifier
import numpy as np

class MLPClassifier_ConstantWeights(MLPClassifier):

    def _init_coef(self, fan_in, fan_out, dtype):
        # Use the initialization method recommended by
        # Glorot et al.
        factor = 6.0
        if self.activation == "logistic":
            factor = 2.0
        init_bound = np.sqrt(factor / (fan_in + fan_out))

        # Generate weights and bias:
        # coef_init = self._random_state.uniform(
        #     -init_bound, init_bound, (fan_in, fan_out)
        # )
        # intercept_init = self._random_state.uniform(-init_bound, init_bound, fan_out)

        coef_init = np.zeros((fan_in, fan_out))
        intercept_init = np.zeros(fan_out)

        increment = init_bound * factor / (fan_in * fan_out)
        mul = 1
        value = 0
        for i in range(fan_in):
            for j in range(fan_out):
                coef_init[i, j] = value * mul
                value += increment
                if value > init_bound:
                    value = -init_bound + (value - init_bound)
                mul = -mul

        for i, v in enumerate(np.linspace(-init_bound, init_bound, fan_out)):
            intercept_init[i] = v
            
        coef_init = coef_init.astype(dtype, copy=False)
        intercept_init = intercept_init.astype(dtype, copy=False)
        return coef_init, intercept_init
    
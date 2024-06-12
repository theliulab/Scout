
from random import shuffle
from sklearn.neural_network import MLPClassifier
from sklearn.ensemble import RandomForestClassifier
from mlp_classifier_constant import MLPClassifier_ConstantWeights
import uuid
import os
# import reader
import ppi
import fdr_utils
import ppi as ppiLib
import pandas as pd

import sys
import json

_nn_ppi = MLPClassifier_ConstantWeights(alpha=0.01,
                    hidden_layer_sizes=(5,),
                    # solver='sgd',
                    solver='lbfgs',
                    # solver = 'adam',
                    max_iter = 1000,
                    learning_rate='adaptive', # constant, invscaling, adaptive
                    learning_rate_init=0.01,
                    n_iter_no_change=50,
                    tol = 1e-5,
                    validation_fraction=0.1,
                    random_state=0,
                    # verbose=True
                    shuffle=False
                    )

_forest = RandomForestClassifier(
    max_depth = 2,
    max_samples = 50,
    min_samples_split=100)

print('Starting python script. Arguments: {0}'.format(sys.argv))

path_csv = sys.argv[1]
path_json = sys.argv[2]

# path_csv = r'C:/Users/milan/source/repos/MilanClasen/MSScout/MSScout/bin/Debug/net6.0-windows/498cac9d-fd02-438b-9652-708a08cc0c16_elements.csv'
# path_json = r'C:/Users/milan/source/repos/MilanClasen/MSScout/MSScout/bin/Debug/net6.0-windows/c66b1d5a-1193-41fc-b449-07516aaf2faa_postParams.json'


f = open(path_json)
params = json.load(f)
f.close()


allPPIs = pd.read_csv(path_csv, low_memory=False)

fdr = params['PPI_FDR']
features = params['PPI_Features']


if(len(allPPIs) == 0):
    print("ERROR: There is no PPI.")
else:
    print('Filtering {0} ppis...'.format(len(allPPIs)))
    ppis = ppiLib.filter_ppis(
        allPPIs, 
        fdr,
        features = features, 
        #clf = _forest
        clf = _nn_ppi
        )

    path_save = fdr_utils.getTempDir() + r'ppiScoring_{0}.csv'.format(str(uuid.uuid4()))
    newDF = ppis[['ID', 'Score']]
    newDF.to_csv(
        path_save, 
        index=False)

    print('p:CSV:{0}'.format(path_save))
    print('Finished running script. Results saved to {0}'.format(path_save))


from random import shuffle
from sklearn.neural_network import MLPClassifier
import uuid
import os
# import reader
import csm_filter
import fdr_utils

import pandas as pd

import sys
import json
from mlp_classifier_constant import MLPClassifier_ConstantWeights

#_nn =  MLPClassifier(alpha=0.01,
#                    hidden_layer_sizes=(10,),
#                    solver='lbfgs',
#                    max_iter = 1000,
#                    learning_rate='adaptive',
#                    learning_rate_init=0.01,
#                    n_iter_no_change=50,
#                    tol = 1e-5,
#                    validation_fraction=0.1,
#                    random_state=0
#                    )

_nn =  MLPClassifier_ConstantWeights(
                    alpha=0.1,
                    hidden_layer_sizes=(10,),
                    solver='adam',
                    max_iter = 1000,
                    learning_rate='adaptive',
                    learning_rate_init=0.1,
                    n_iter_no_change=50,
                    tol = 1e-5,
                    validation_fraction=0.1,
                    random_state=0,
                    shuffle=False
                    )


print('Starting python script. Arguments: {0}'.format(sys.argv))

path_csv = sys.argv[1]
path_json = sys.argv[2]

# path_csv = r'C:/Users/milan/source/repos/MilanClasen/MSScout/MSScout/bin/Debug/net6.0-windows/498cac9d-fd02-438b-9652-708a08cc0c16_elements.csv'
# path_json = r'C:/Users/milan/source/repos/MilanClasen/MSScout/MSScout/bin/Debug/net6.0-windows/c66b1d5a-1193-41fc-b449-07516aaf2faa_postParams.json'


f = open(path_json)
params = json.load(f)
f.close()


allCSMs = pd.read_csv(path_csv, low_memory=False)

fdr = params['CSM_FDR']
is_looplink_filter = params['IsLooplinkFilter'];
print('Is looplink: ' + str(is_looplink_filter))
features = params['CSM_Features'];
print('Filtering {0} csms...'.format(len(allCSMs)))


scoredCSMs = csm_filter.standard_pipeline(
    allCSMs, 
    features, 
    1, 
    is_looplink = is_looplink_filter,
    diogo_mode = False,
    clf = _nn,
    sep_mode = False,
    )

path_save = fdr_utils.getTempDir() + r'csmScoring_{0}.csv'.format(str(uuid.uuid4()))
if(scoredCSMs is None):
    print("ERROR: There is no CSM")
else:
    print("INFO: CSMs OK")
    newDF = scoredCSMs[['ID', 'Score']]
    newDF.to_csv(
        path_save, 
        index=False)

print('p:CSV:{0}'.format(path_save))
print('Finished running script. Results saved to {0}'.format(path_save))

# -*- coding: utf-8 -*-
import pandas as pd
import numpy as np

from sklearn.model_selection import train_test_split
from sklearn.model_selection import cross_val_score
from sklearn.preprocessing import MinMaxScaler
from sklearn.ensemble import RandomForestClassifier
from sklearn.naive_bayes import GaussianNB
from sklearn import svm
from sklearn.neighbors import KNeighborsClassifier
from sklearn.neural_network import MLPClassifier

from sklearn.model_selection import StratifiedKFold
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import cross_val_score

import fdr_utils
import re
 


def filter_respairs(respairs, fdr, features = None, clf = None, test_size = 0):

    print('Using features: ' + str(features))
    print('clf: ' + str(type(clf)))
    X = respairs[ features ].to_numpy()
    X = MinMaxScaler().fit(X).transform(X)
    # X = StandardScaler().fit(X).transform(X)
    
    y = np.array([1 if d == False else 0 for d in respairs['IsDecoy']])
    
    # if test_size == 0:
    X_train = X
    y_train = y
    # else:
    #     X_train, X_test, y_train, y_test = train_test_split(X, y, test_size=0.01, stratify=y)
        
    clf.fit(X_train, y_train)
    
    if(hasattr(clf, 'predict_proba')):
        scores = [v[1] for v in clf.predict_proba(X)]
    else:
        scores = [s for s in clf.predict(X)]
    # scores = [v[1] for v in clf.decision_function(X)]
    
    # if test_size != 0:
    #     print("Score Test: {0}".format(clf.score(X_test, y_test)))
    #     print("Score Train: {0}".format(clf.score(X_train, y_train)))
    print("Score All: {0}".format(clf.score(X, y)))
    

    df = respairs.copy()
    df['Score'] = scores
    # df['IsDecoy'] = [False if c == 0 else True for c in df['ClassNum']]
    (fdr_real, ids, _, threshold_score)= fdr_utils.filter_df(df, fdr, 'Score', 'IsDecoy')
    
    
    # (fdr_unlab, unlab_ctt) = fdr_utils.get_unlabelled_fdr(ids, 'Unlabelled_', True )
    decoy_ctt = len(ids[ids['IsDecoy'] == True])
    
    
    print("FDR: {:.2%}. Decoy Count: {}".format(fdr_real, decoy_ctt))
    # print("FDR Unlabelled: {:.2%}. Unlab Count: {}".format(fdr_unlab, unlab_ctt))
    print("IDs: {0}".format(len(ids)))
    print("Threshold Score: {0}".format(threshold_score))
    
    return (df, ids, fdr_real, threshold_score)
    

def filter_respairs_boost(respairs, fdr, features = None, clf = None, test_size = 0):

    print('Using features: ' + str(features))
    print('clf: ' + str(type(clf)))

    key_score = 'BestPoisson'
    decoys = respairs[(respairs['IsDecoy'] == True) & (respairs[key_score] > 0)]
    targets = respairs[(respairs['IsDecoy'] == False) & (respairs[key_score] > 0)]

    ######### TARGETS ########

    #order by key_score 
    targets = targets.sort_values(by=[key_score], ascending=False)

    #select the first two quartile with the best scores
    quartil_target = int (len(targets) / 2)
    if quartil_target == 0:
        quartil_target = 1
    targets = targets.head(quartil_target)

    #mean score of the best quartil targets
    mean_targets = (targets[key_score].values[0] + targets[key_score].values[len(targets) - 1]) / 2

    #select all targets with a score greater than mean_targets
    targets = targets[(targets[key_score] >= mean_targets)]

    #filter targets by bestDDPScore
    csms_threshold_target = (targets['BestDDPScore'].values[0] + targets['BestDDPScore'].values[len(targets) - 1]) / 2
    targets = targets[(targets['BestDDPScore'] >= csms_threshold_target)]

    ######### TARGETS ########

    ######### DECOYS ########
    #order by key_score 
    decoys = decoys.sort_values(by=[key_score], ascending=True)

    #compute first and third quartile to remove outliers
    Q1 = np.percentile(decoys[key_score], 25,
                       interpolation = 'midpoint')

    Q3 = np.percentile(decoys[key_score], 75,
                       interpolation = 'midpoint')
    #compute interquarile
    IQR = Q3 - Q1
    #determine the threshold to remove outliers (upper bound)
    upper_bound = Q3 + 1.5 * IQR

    #select all targets with a score less than upper_bound
    decoys = decoys[(decoys[key_score] <= upper_bound)]

    #filter targets by PPM
    #sort by BestDDPScore
    decoys = decoys.sort_values(by=['BestDDPScore'], ascending=True)

    csms_threshold_decoy = (decoys['BestDDPScore'].values[0] + decoys['BestDDPScore'].values[len(decoys) - 1]) / 2
    decoys = decoys[(decoys['BestDDPScore'] <= csms_threshold_decoy)]

    #sort by key_score
    decoys = decoys.sort_values(by=[key_score], ascending=True)

    ######### DECOYS ########

    min_len = min(len(targets), len(decoys))
    decoys = decoys.head(min_len)
    targets = targets.head(min_len)

    new_resPairs = pd.concat([targets,decoys])
    new_resPairs = new_resPairs.reset_index()

    #All candidates
    X = respairs[ features ].to_numpy()
    X = MinMaxScaler().fit(X).transform(X)
   
    y = np.array([0 if d is True else 1 for d in respairs['IsDecoy']])

    # Train
    X_train = new_resPairs[ features ].to_numpy()
    X_train = MinMaxScaler().fit(X_train).transform(X_train)
    y_train = np.array([0 if d is True else 1 for d in new_resPairs['IsDecoy']])
    
    clf.fit(X_train, y_train)

    if(hasattr(clf, 'predict_proba')):
        scores = [v[1] for v in clf.predict_proba(X)]
    else:
        scores = [s for s in clf.predict(X)]
    
    print("Score All: {0}".format(clf.score(X, y)))
    

    df = respairs.copy()
    df['Score'] = scores
    # df['IsDecoy'] = [False if c == 0 else True for c in df['ClassNum']]
    (fdr_real, ids, _, threshold_score)= fdr_utils.filter_df(df, fdr, 'Score', 'IsDecoy')
    
    
    # (fdr_unlab, unlab_ctt) = fdr_utils.get_unlabelled_fdr(ids, 'Unlabelled_', True )
    decoy_ctt = len(ids[ids['IsDecoy'] == True])
    
    
    print("FDR: {:.2%}. Decoy Count: {}".format(fdr_real, decoy_ctt))
    # print("FDR Unlabelled: {:.2%}. Unlab Count: {}".format(fdr_unlab, unlab_ctt))
    print("IDs: {0}".format(len(ids)))
    print("Threshold Score: {0}".format(threshold_score))
    
    return (df, ids, fdr_real, threshold_score)
    
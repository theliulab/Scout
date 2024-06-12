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
 

def filter_csms_diogo(csms, fdr, features, clf, add_scores_to_df = True, test_size = 0):    
       
    print('Using features: ' + str(features))
    print('Filtering {0} csms.'.format(len(csms)))
    print('  Targets: {0}.'.format(len(csms[csms['IsDecoy'] == False])))
    print('  Decoys: {0}.'.format(len(csms[csms['IsDecoy'] == True])))
    print('clf: ' + str(type(clf)))
    
    key_score = 'PoissonScore'
    if not key_score in csms:
        print("NOT");
    
    
    decoys = csms[csms['IsDecoy'] == True]
    decoys = decoys.reset_index(drop=True)
    targets = csms[[(is_decoy == True) & (score > 0) for is_decoy, score in zip(csms['IsDecoy'], csms[key_score])]]
    targets = targets.reset_index(drop=True)
    ######### TARGETS ########

    #order by xlinkxGroupedScore 
    targets = targets.sort_values(by=[key_score], ascending=False)

    #select the first quartil with the best scores
    quartil_target = int (len(targets) / 4)
    targets = targets.head(quartil_target)
    print(f'DIOGO MODE - Targets: {quartil_target}')
    quartile_mean_mode = False
    if quartile_mean_mode :
        #mean score of the best quartil targets
        mean_targets = (targets[key_score].values[0] + targets[key_score].values[len(targets) - 1]) / 2
        # mean_targets = sum(targets[key_score].values) / len(targets)
    
        #select all targets with a score greater than mean_targets
        targets = targets[(targets[key_score] > mean_targets)]
        print('DIOGO MODE - Remaining targets: {0} . Best quartile mean: {1}'.format(len(targets), mean_targets))
    else:
        #mean score of the best quartil targets
        mean_targets = targets[key_score].values[-1]
    
        #select all targets with a score greater than mean_targets
        targets = targets[(targets[key_score] > mean_targets)]
        print('DIOGO MODE - Remaining targets: {0} . Score threshold: {1}'.format(len(targets), mean_targets))
    
    
    #filter targets by PPM
    ppm_threshold_target = (targets["DiffPPM"].values[0] + targets["DiffPPM"].values[len(targets) - 1])
    if ppm_threshold_target < 5:
        ppm_threshold_target = 5
    
    targets = targets[(targets['DiffPPM'] < ppm_threshold_target)]

    ######### TARGETS ########

    ######### DECOYS ########
    #order by xlinkxGroupedScore 
    decoys = decoys.sort_values(by=[key_score], ascending=False)

    #select the first quartil with the best scores
    quartil_decoy = int (len(decoys) / 4)
    decoys = decoys.head(quartil_decoy)
    print(f'DIOGO MODE - Decoys: {quartil_decoy}')
    
    #mean score of the best quartil targets
    mean_decoys = (decoys[key_score].values[0] + decoys[key_score].values[len(decoys) - 1]) / 2

    print(f'DIOGO MODE - Mean Decoys: {mean_decoys}')

    #select all targets with a score greater than mean_targets
    decoys = decoys[(decoys[key_score] < mean_decoys)]

    #filter decoys by PPM
    ppm_threshold_decoy = (decoys["DiffPPM"].values[0] + decoys["DiffPPM"].values[len(decoys) - 1]) / 2
    decoys = decoys[(decoys['DiffPPM'] > ppm_threshold_decoy)]
    
    ######### DECOYS ########
    
    min_len = min(len(targets), len(decoys))
    decoys = decoys.head(min_len)
    targets = targets.head(min_len)
    new_csms = pd.concat([targets,decoys])
    new_csms = new_csms.reset_index()
    
    #All candidates
    X = csms[ features ].to_numpy()
    X = MinMaxScaler().fit(X).transform(X)
    # X = StandardScaler().fit(X).transform(X)
    
    # y = np.array([1 if d == 0 else 0 for d in csms['ClassNum']])
    y = np.array([0 if d is True else 1 for d in csms['IsDecoy']])
    
    # Train
    X_train = new_csms[ features ].to_numpy()
    X_train = MinMaxScaler().fit(X_train).transform(X_train)
    y_train = np.array([0 if d is True else 1 for d in new_csms['IsDecoy']])
     
    
    # X = csms[ features ].to_numpy()
    # X = MinMaxScaler().fit(X).transform(X)
    # X = StandardScaler().fit(X).transform(X)
    
    # y = np.array([1 if d == 0 else 0 for d in csms['ClassNum']])
    
    # X_train = X
    # y_train = y
    # if test_size == 0:
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
    
    if(add_scores_to_df == True):
        csms['Score'] = scores
        
    df = csms.copy()
    df['Score'] = scores
    (fdr_real, ids, _, threshold_score) = fdr_utils.filter_df(df, fdr, 'Score', 'IsDecoy')
    
    (fdr_unlab, unlab_ctt) = fdr_utils.get_unlabelled_fdr(ids, 'Unlabelled_', True )
    decoy_ctt = len(ids[ids['IsDecoy'] == True])
    
    print("FDR: {:.2%}. Decoy Count: {}".format(fdr_real, decoy_ctt))
    print("FDR Unlabelled: {:.2%}. Unlab Count: {}".format(fdr_unlab, unlab_ctt))
    print("IDs: {0}".format(len(ids)))
    print("Threshold Score: {0}".format(threshold_score))
    
    return (df, ids, fdr_real, threshold_score)

def filter_csms(csms, fdr, features, clf, add_scores_to_df = True, test_size = 0):    
       
    print('Using features: ' + str(features))
    print('Filtering {0} csms.'.format(len(csms)))
    print('  Targets: {0}.'.format(len(csms[csms['IsDecoy'] == False])))
    print('  Decoys: {0}.'.format(len(csms[csms['IsDecoy'] == True])))
    print('clf: ' + str(type(clf)))
    X = csms[ features ].to_numpy()
    # X = MinMaxScaler().fit(X).transform(X)
    X = StandardScaler().fit(X).transform(X)
    
    y = np.array([0 if d is True else 1 for d in csms['IsDecoy']])
    #y = np.array([1 if d == 0 else 0 for d in csms['ClassNum']])
    
    X_train = X
    y_train = y
    # if test_size == 0:
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
    
    if(add_scores_to_df == True):
        csms['Score'] = scores
        
    df = csms.copy()
    df['Score'] = scores
    
    
    return df

def filter_csms_looplink(csms, fdr, features, clf, add_scores_to_df = True, test_size = 0):    
    
    print('Original features: ' + str(features))
    print('Filtering {0} csms.'.format(len(csms)))
    print('  Targets: {0}.'.format(len(csms[csms['IsDecoy'] == False])))
    print('  Decoys: {0}.'.format(len(csms[csms['IsDecoy'] == True])))
    print('clf: ' + str(type(clf)))
    
    #key_score = 'PoissonScore'
    key_score = 'XLScore'
    if not key_score in csms:
        return (csms, csms, 0, 0)
    
    
    decoys = csms[csms['IsDecoy'] == True]
    decoys = decoys.reset_index(drop=True)
    targets = csms[[(is_decoy == True) & (score > 0) for is_decoy, score in zip(csms['IsDecoy'], csms[key_score])]]
    targets = targets.reset_index(drop=True)
    ######### TARGETS ########

    #order by xlinkxGroupedScore 
    targets = targets.sort_values(by=[key_score], ascending=False)

    #select the first quartil with the best scores
    quartil_target = int (len(targets) / 4)
    targets = targets.head(quartil_target)
    print(f'LOOPLINK MODE - Targets: {quartil_target}')
    quartile_mean_mode = True
    if quartile_mean_mode :
        #mean score of the best quartil targets
        mean_targets = (targets[key_score].values[0] + targets[key_score].values[len(targets) - 1]) / 2
        # mean_targets = sum(targets[key_score].values) / len(targets)
    
        #select all targets with a score greater than mean_targets
        targets = targets[(targets[key_score] > mean_targets)]
        print('LOOPLINK MODE - Remaining targets: {0} . Best quartile mean: {1}'.format(len(targets), mean_targets))
    else:
        #mean score of the best quartil targets
        mean_targets = targets[key_score].values[-1]
    
        #select all targets with a score greater than mean_targets
        targets = targets[(targets[key_score] > mean_targets)]
        print('LOOPLINK MODE - Remaining targets: {0} . Score threshold: {1}'.format(len(targets), mean_targets))
    
    
    #filter targets by PPM
    if "DiffPPM" in csms:
        ppm_threshold_target = (targets["DiffPPM"].values[0] + targets["DiffPPM"].values[len(targets) - 1]) / 2
        if ppm_threshold_target < 5:
            ppm_threshold_target = 5
        
        targets = targets[(targets['DiffPPM'] < ppm_threshold_target)]
        
    #filter targets by MinRPinProteins
    if "MinUniqueRPsInInterProteins" in csms:
        minRPInProteins_threshold_targets = (targets["MinUniqueRPsInInterProteins"].values[0] + targets["MinUniqueRPsInInterProteins"].values[len(targets) - 1]) / 2
        targets = targets[(targets['MinUniqueRPsInInterProteins'] > (minRPInProteins_threshold_targets - 1))]

    ######### TARGETS ########

    ######### DECOYS ########
    #order by xlinkxGroupedScore 
    decoys = decoys.sort_values(by=[key_score], ascending=False)

    #select the first quartil with the best scores
    quartil_decoy = int (len(decoys) / 4)
    decoys = decoys.head(quartil_decoy)
    print(f'LOOPLINK MODE - Decoys: {quartil_decoy}')
    
    #mean score of the best quartil targets
    mean_decoys = (decoys[key_score].values[0] + decoys[key_score].values[len(decoys) - 1]) / 2

    print(f'LOOPLINK MODE - Mean Decoys: {mean_decoys}')

    #select all targets with a score greater than mean_targets
    decoys = decoys[(decoys[key_score] < mean_decoys)]

    #filter decoys by PPM
    if "DiffPPM" in csms:
        ppm_threshold_decoy = (decoys["DiffPPM"].values[0] + decoys["DiffPPM"].values[len(decoys) - 1]) / 2
        decoys = decoys[(decoys['DiffPPM'] > ppm_threshold_decoy)]
        
    #filter decoys by MinRPinProteins
    if "MinUniqueRPsInInterProteins" in csms:
        minRPInProteins_threshold_decoy = (decoys["MinUniqueRPsInInterProteins"].values[0] + decoys["MinUniqueRPsInInterProteins"].values[len(decoys) - 1]) / 2
        decoys = decoys[(decoys['MinUniqueRPsInInterProteins'] < (minRPInProteins_threshold_decoy + 1))]
    
    ######### DECOYS ########
    
    min_len = min(len(targets), len(decoys))
    decoys = decoys.head(min_len)
    targets = targets.head(min_len)
    new_csms = pd.concat([targets,decoys])
    new_csms = new_csms.reset_index()
    
    features = ['XLScore','PoissonScore' ]
    print('Using features: ' + str(features))
    
    #All candidates
    X = csms[ features ].to_numpy()
    X = MinMaxScaler().fit(X).transform(X)
    # X = StandardScaler().fit(X).transform(X)
    
    # y = np.array([1 if d == 0 else 0 for d in csms['ClassNum']])
    y = np.array([0 if d is True else 1 for d in csms['IsDecoy']])
    
    # Train
    X_train = new_csms[ features ].to_numpy()
    X_train = MinMaxScaler().fit(X_train).transform(X_train)
    y_train = np.array([0 if d is True else 1 for d in new_csms['IsDecoy']])
     
        
    clf.fit(X_train, y_train)
    
    if(hasattr(clf, 'predict_proba')):
        scores = [v[1] for v in clf.predict_proba(X)]
    else:
        scores = [s for s in clf.predict(X)]
        
    print("Score All: {0}".format(clf.score(X, y)))
    
    scores = [1.0 - element for element in scores]
    
    if(add_scores_to_df == True):
        csms['Score'] = scores

    df = csms.copy()
    df['Score'] = scores
    (fdr_real, ids, _, threshold_score) = fdr_utils.filter_df(df, fdr, 'Score', 'IsDecoy')
    decoy_ctt = len(ids[ids['IsDecoy'] == True])
    
    print("FDR: {:.2%}. Decoy Count: {}".format(fdr_real, decoy_ctt))
    print("IDs: {0}".format(len(ids)))
    print("Threshold Score: {0}".format(threshold_score))
    
    return (df, ids, fdr_real, threshold_score)

def standard_pipeline(df, 
                      features = None, 
                      fdr_csms = 0.01, 
                      clf = None, 
                      thresholds = None,
                      is_looplink = False,
                      diogo_mode = True,
                      sep_mode = False):
    
    newDF = df.copy()
            
    if diogo_mode == True:
        (df, _,_,_) = filter_csms_diogo(newDF, fdr_csms, features = features, clf = clf)
    elif is_looplink == True:
        (df, _,_,_) = filter_csms_looplink(newDF, fdr_csms, features = features, clf = clf)
    else:
        df = filter_csms(newDF, fdr_csms, features = features, clf = clf)
    

    return df




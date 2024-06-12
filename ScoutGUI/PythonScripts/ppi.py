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
import math
import re



def filter_ppis(allPPIs, fdr, features = None, clf = None, test_size = 0):
    
    print('Using features: ' + str(features))
    print('clf: ' + str(type(clf)))
    X = allPPIs[ features ].to_numpy()
    # X = MinMaxScaler().fit(X).transform(X)
    X = StandardScaler().fit(X).transform(X)
    
    y = np.array([1 if d == False else 0 for d in allPPIs['IsDecoy']])
    
    # if test_size == 0:
    X_train = X
    y_train = y

    clf.fit(X_train, y_train)
    
    if(hasattr(clf, 'predict_proba')):
        scores = [v[1] for v in clf.predict_proba(X)]
    else:
        scores = [s for s in clf.predict(X)]
    
    print("Score All: {0}".format(clf.score(X, y)))
    

    df = allPPIs.copy()
    df['Score'] = scores
   
    
    # print("FDR Unlabelled: {:.2%}. Unlab Count: {}".format(fdr_unlab, unlab_ctt))
    
    
    return df
    
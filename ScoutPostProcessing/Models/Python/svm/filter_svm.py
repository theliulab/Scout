# -*- coding: utf-8 -*-
import pandas as pd
import numpy as np

from sklearn.preprocessing import MinMaxScaler
from sklearn import svm

def get_scores_svm(df, features, decoy_column = 'ClassNum', decoy_value = 0):
    
    clf = svm.SVC(kernel='linear', C=1,  probability=True)

    print('Using features: ' + str(features))
    X = df[ features ].to_numpy()
    X = MinMaxScaler().fit(X).transform(X)
    
    y = np.array([1 if d == decoy_value else 0 for d in df[decoy_column]])

    clf.fit(X, y)

    scores = [v[1] for v in clf.predict_proba(X)]
    
    return scores


def filter_svm(df, features, 
               decoy_column = 'ClassNum', decoy_value  = 0, 
               fdr_threshold = 0.05,
               unlabelled_tag = None):

    df['ClfScore'] = get_scores_svm(df, features, decoy_column, decoy_value)
    df['IsDecoy'] = [False if c == decoy_value else True for c in df[decoy_column]]

    (fdr_real, ids, threshold_score)= filter_df(df, fdr_threshold, 'ClfScore', 'IsDecoy')
    
    if(unlabelled_tag != None):
        fdr_unlab = get_unlabelled_fdr(ids, 'Unlabelled_', True)

    return (df, fdr_real, ids, threshold_score)

# -*- coding: utf-8 -*-
"""
Created on Mon Aug 15 18:38:58 2022

@author: milan
"""

import sys
import pandas as pd
import uuid

from sklearn.preprocessing import MinMaxScaler
import numpy as np

from sklearn.naive_bayes import GaussianNB

import fdr_utils


def get_scores(df, features, y):
    
    X = df[ features ].to_numpy()
    X = MinMaxScaler().fit(X).transform(X)

    _nb = GaussianNB()
    clf = _nb
   
    clf.fit(X, y)
    
    scores = [v[1] for v in clf.predict_proba(X)]
    
    return scores
    

def final_scoring(df, alpha_features, beta_features):

    #alpha_features = [
    #    'AlphaScore',
    #    'AlphaTagScore',
    #    'AlphaDeltaCN'
    #    ]
    
    #beta_features = [
    #    'BetaScore',
    #    'BetaTagScore',
    #    'BetaDeltaCN',
    #    ]
    
    y_alpha = np.array([1 if d == 'AlphaTarget' or d == 'FullTarget' else 0 for d in df['Class']])
    y_beta = np.array([1 if d == 'BetaTarget' or d == 'FullTarget' else 0 for d in df['Class']])
    
    newDF = df.copy()
    
    newDF['AlphaFinalScore'] = get_scores(df, alpha_features, y_alpha)
    newDF['BetaFinalScore'] = get_scores(df, beta_features, y_beta)
    newDF['MinFinalScore'] = [min(a,b) for (a,b) in zip(newDF['AlphaFinalScore'],newDF['BetaFinalScore'])]
    #newDF['MinZScore'] = [min(a,b) for (a,b) in zip(newDF['AlphaZScore'],newDF['BetaZScore'])]
    
    return newDF




print('Starting python script. Arguments: {0}'.format(sys.argv))

path_csv = sys.argv[1]
# path_csv = r'C:/Users/milan/source/repos/MilanClasen/MSScout/MSScout/bin/Debug/net6.0-windows/4cad356b-86b3-46ce-b63e-5c6998065ad6_elements.csv'

allCSMs = pd.read_csv(path_csv, low_memory=False)

alpha_features = [c for c in allCSMs.columns if 'Alpha' in c]
beta_features = [c for c in allCSMs.columns if 'Beta' in c]

newDF = final_scoring(allCSMs, alpha_features, beta_features)

path_save = fdr_utils.getTempDir() + r'final_scoring_{0}.csv'.format(str(uuid.uuid4()))
newDF = newDF[['ID', 'MinFinalScore']]
newDF.to_csv(
    path_save, 
    index=False)

print('p:CSV:{0}'.format(path_save))
print('Finished running script. Results saved to {0}'.format(path_save))
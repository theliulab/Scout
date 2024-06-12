import sys
import os

import pandas as pd
import numpy as np

from sklearn.preprocessing import MinMaxScaler
from sklearn import svm



def main():
 
    # total arguments
    n = len(sys.argv)
    print("Total arguments passed:", n)
 
    print("Run folder: " + str(os.getcwd()))
    cwd = os.getcwd()

    # Arguments passed
    print("\nName of Python script:", sys.argv[0])
 
    print("\nArguments passed:", end = " ")
    for i in range(1, n):
        print(sys.argv[i], end = " ")
     
    df_path = sys.argv[1]
    output_path = sys.argv[2]

    features = []
    for i in range(3, n):
        features.append(sys.argv[i])

    df = simple_svm_scoring(df_path, features)
    df.to_csv(output_path)


def apply_thresholds(df):
    df['DiffPPM'] = [abs(a - b) for a,b in zip(df['AlphaPPM'], df['BetaPPM'])]
    df = df[df['DiffPPM'] < 15]
    # df = df[df['AlphaPPM'] < 15]
    # df = df[df['BetaPPM'] < 15]
    
    return df


def simple_svm_scoring(path, features):
    df = pd.read_csv(path, 
                    sep=',', 
                    index_col=False)
    print('CSV loaded. Rows: ' + str(len(df)))
    
    df = df.fillna(0)
    df = apply_thresholds(df)

    head = df.head()
    print(head)

    (df, fdr_real, ids, threshold_score) = filter_svm(df, features, fdr_threshold=0.01)

    print("FDR: {:.2%}".format(fdr_real))
    #print("FDR Unlabelled: {:.2%}".format(cfdr_unlab))
    print("IDs: {0}".format(len(ids)))
    print("Threshold Score: {0}".format(threshold_score))

    return df


def filter_df(df, threshold_fdr, score_column, is_decoy_column):
    sortedDF = df.sort_values(by=score_column, ascending=True)
    sortedDF = sortedDF.reset_index().drop('index', axis=1)

    decoy_ctt = len(sortedDF[sortedDF[is_decoy_column]])
    
    cutoff_i = 0
    
    for i in range(0, len(sortedDF)):
        
        fdr = decoy_ctt / (len(sortedDF) - i)
        
        if(fdr < threshold_fdr):
            cutoff_i = i
            break
        
        if sortedDF[is_decoy_column].iloc[i] == True:
            decoy_ctt -= 1
            
    threshold_score = sortedDF.iloc[cutoff_i][score_column]
   
    ids = sortedDF.iloc[cutoff_i : len(sortedDF) - 1]
    
    return fdr, ids, threshold_score


def get_unlabelled_fdr(ids, unlabelled_tag = 'Unlabelled_'):
    
    isUnlab = [unlabelled_tag in csm[0] or unlabelled_tag in csm[1]  for csm in zip(ids['AlphaMappings'], ids['BetaMappings'])]
    
    bad = ids[isUnlab]
    
    return len(bad)/len(ids)



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


main()
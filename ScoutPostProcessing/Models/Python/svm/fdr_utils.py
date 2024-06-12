import pandas as pd
import numpy as np

from statistics import stdev
from statistics import NormalDist


import matplotlib.pyplot as plt


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
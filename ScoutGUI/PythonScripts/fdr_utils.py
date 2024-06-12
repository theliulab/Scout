import tempfile
import pandas as pd
import numpy as np
import itertools

from statistics import stdev
from statistics import NormalDist

# from fdr_plotting import hist_feature

import matplotlib.pyplot as plt
import math

import re

from sklearn.model_selection import train_test_split
from sklearn.model_selection import cross_val_score
from sklearn.ensemble import RandomForestClassifier
from sklearn.preprocessing import MinMaxScaler
from sklearn.naive_bayes import GaussianNB
from sklearn import svm
from sklearn.model_selection import StratifiedKFold
from sklearn.preprocessing import StandardScaler
from sklearn.model_selection import cross_val_score


def getTempDir():
    return tempfile.gettempdir() + '\\'


def filter_df(df, threshold_fdr, score_column = 'Score', is_decoy_column = 'IsDecoy', console = False):
    if(len(df) == 0):
        return 0, df, 0, 0
   
    items = [(index, score, decoy) for index, score, decoy in zip(df.index, df[score_column].values, df[is_decoy_column].values)]

    items = sorted( items, key = lambda x: x[1], reverse = False)
    decoy_ctt = len([item for item in items if item[2] == True])

    cutoff_i = 0


    for i in range(0, len(items)):
        
        fdr = decoy_ctt / (len(items) - i)
        
        if(fdr < threshold_fdr):
            cutoff_i = i
            break
        
        if items[i][2] == True:
            decoy_ctt -= 1
            
    threshold_score = items[cutoff_i][1]


    ids = df[df[score_column] >= threshold_score]
    ids = ids.reset_index(drop=True)

    if(console == True):
        print('Filtered {0} items at {{:.1%}} FDR.\n  Remaining items: {2} items.\n  Threshold score: {3}'.format(len(items), threshold_fdr, len(ids), threshold_score))
    
    return fdr, ids, cutoff_i, threshold_score


def filter_df_separately(df, fdr, score_column = 'Score', is_decoy_column = 'IsDecoy', console = False):
    
    df_inter_all = df[df['Inter'] == True]
    df_inter_all = df_inter_all.reset_index(drop=True)
    df_intra_all = df[df['Inter'] == False]
    df_intra_all = df_intra_all.reset_index(drop=True)

    _, df_inter, _, thresh_inter = filter_df(df_inter_all, fdr, score_column, is_decoy_column,  console)
    df_inter = df_inter.reset_index(drop=True)
    print( 'Filtered {0} inter link items into {1} items'.format(len(df_inter_all), len(df_inter)) ) 
    
    _, df_intra, _, _ = filter_df(df_intra_all, fdr, score_column, is_decoy_column, console)
    df_intra = df_intra.reset_index(drop=True)
    print( 'Filtered {0} intra link items into {1} items'.format(len(df_intra_all), len(df_intra)) ) 

    df_filtered = pd.concat([df_intra, df_inter])
    df_filtered = df_filtered.reset_index(drop=True)
        
    return df_filtered, df_inter, df_intra


def filter_df_separately_shared_decoys(df, fdr, score_column = 'Score', is_decoy_column = 'IsDecoy', console = False):
    
    decoys = df[df['IsDecoy'] == True]
    decoys = decoys.reset_index(drop = True)
    targets = df[df['IsDecoy'] == False]
    targets = targets.reset_index(drop = True)
    targets_inter = targets[targets['Inter'] == True]
    targets_inter = targets_inter.reset_index(drop = True)
    targets_intra = targets[targets['Inter'] == False]
    targets_intra = targets_intra.reset_index(drop = True)

    df_inter_all = pd.concat([targets_inter, decoys])
    df_inter_all = df_inter_all.reset_index(drop=True)
    df_intra_all = pd.concat([targets_intra, decoys])
    df_intra_all = df_intra_all.reset_index(drop=True)

    _, df_inter, _, thresh_inter = filter_df(df_inter_all, fdr, score_column, is_decoy_column,  console)
    df_inter = df_inter.reset_index(drop=True)
    print( 'Filtered {0} inter link items into {1} items'.format(len(df_inter_all), len(df_inter)) ) 
    
    _, df_intra, _, _ = filter_df(df_intra_all, fdr, score_column, is_decoy_column, console)
    df_intra = df_intra.reset_index(drop=True)
    print( 'Filtered {0} intra link items into {1} items'.format(len(df_intra_all), len(df_intra)) ) 

    # filtered_intra_targets = df_intra[df_intra['IsDecoy'] == False]
    # filtered_inter_targets = df_inter[df_inter['IsDecoy'] == False]

    df_filtered = pd.concat([df_intra, df_inter])
    df_filtered = df_filtered.reset_index(drop=True)
        
    return df_filtered, df_inter, df_intra


#%%


def get_scores(df, features, y):
    
    X = df[ features ].to_numpy()
    X = MinMaxScaler().fit(X).transform(X)

    _nb = GaussianNB()
    clf = _nb
   
    clf.fit(X, y)
    
    scores = [v[1] for v in clf.predict_proba(X)]
    
    return scores
    

def final_scoring(df):

    alpha_features = [
        'AlphaScore',
        'AlphaTagScore',
        'AlphaDeltaCN'
        ]
    
    beta_features = [
        'BetaScore',
        'BetaTagScore',
        'BetaDeltaCN',
        ]
    
    y_alpha = np.array([1 if d == 'AlphaTarget' or d == 'FullTarget' else 0 for d in df['Class']])
    y_beta = np.array([1 if d == 'BetaTarget' or d == 'FullTarget' else 0 for d in df['Class']])
    
    newDF = df.copy()
    
    newDF['AlphaFinalScore'] = get_scores(df, alpha_features, y_alpha)
    newDF['BetaFinalScore'] = get_scores(df, beta_features, y_beta)
    newDF['MinFinalScore'] = [min(a,b) for (a,b) in zip(newDF['AlphaFinalScore'],newDF['BetaFinalScore'])]
    #newDF['MinZScore'] = [min(a,b) for (a,b) in zip(newDF['AlphaZScore'],newDF['BetaZScore'])]
    
    return newDF


#%%
def get_target_decoy_unlabelled(mapping_series, decoy_tag = "Reverse", unlabelled_tag = "Unlabelled"):
    
    classes = []
    for i in range(0, len(mapping_series)):
        split = mapping_series.iloc[i].split(';')
        
        targets = [s for s in split if decoy_tag not in s and unlabelled_tag not in s]
        if any(targets):
            classes.append('Target')
            continue
        
        decoys = [s for s in split if decoy_tag in s]
        if any(decoys):
            classes.append('Decoy')
            continue
        
        decoys = [s for s in split if unlabelled_tag in s]
        if any(decoys):
            classes.append('Unlabelled')
            continue
        
        classes.append('?')
        
    
    return classes
    


def measure_similarities(df1, df2, feature, bins = 100, print_results = True):
    
    
    def kl_divergence(p, q): 
        return np.sum(p*np.log(p/q))

    def js_divergence(p, q):
        m = (1./2.)*(p + q)
        return (1./2.)*kl_divergence(p, m) + (1./2.)*kl_divergence(q, m)
    
    def get_target_only_area(target, decoy):
        area_per_bin = [t - d if t > d else 0 for t, d in zip(target, decoy)]
        return sum(area_per_bin)
        
    def get_gaussian_overlap(p, q):
        mean_q = sum(q)/len(q)
        mean_p = sum(p)/len(p)
        
        std_q = stdev(q)
        std_p = stdev(p)
        
        return NormalDist(mu=mean_q, sigma=std_q).overlap(NormalDist(mu=mean_p, sigma=std_p))
    
    dist1 = df1[feature]
    dist2 = df2[feature]
    
    vmin = min( [min(dist1), min(dist2)] )
    vmax = max( [max(dist1), max(dist2)] )
    step = (vmax - vmin) / bins
    bins= [vmin + v*step for v in range(0,bins+1)]
    
    dist1 = np.histogram(dist1, bins = bins)[0]
    dist2 = np.histogram(dist2, bins = bins)[0]
    
    ta = get_target_only_area(dist1, dist2)/sum(dist1)
    go = get_gaussian_overlap(dist1, dist2)
    
    non_zeroes = [(v1, v2) for v1, v2 in zip(dist1, dist2) if v1 != 0 and v2 != 0]
    dist1_pdf = np.array([v[0] for v in non_zeroes])
    dist2_pdf = np.array([v[1] for v in non_zeroes])
    
    dist1_pdf = dist1_pdf/sum(dist1_pdf)
    dist2_pdf = dist2_pdf/sum(dist2_pdf)
    
    kl12 = kl_divergence(dist1_pdf, dist2_pdf)
    kl21 = kl_divergence(dist2_pdf, dist1_pdf)
    js = js_divergence(dist1_pdf, dist2_pdf)
    
    if print_results is True:
        print("Feature: " + feature)
        print("KL Divergence 1 to 2: " + str(kl12))
        print("KL Divergence 2 to 1: " + str(kl21))
        print("JS Divergence: " + str(js))
        print("Gaussian Overlap: " + str(go))
        print("Exclusive Area: " + str(ta))
    
    return kl12, kl21, js, ta, go
    

def check_unlabelled(ids, unlabelled_tag):
    return [csm[0].startswith(unlabelled_tag) or csm[1].startswith(unlabelled_tag) for csm in zip(ids['AlphaMappings'], ids['BetaMappings'])]


def get_unlabelled_fdr(ids, unlabelled_tag = 'Unlabelled_', mark_df = True):
    
    isUnlab = check_unlabelled(ids, unlabelled_tag)
        
    if(mark_df == False):
        ids['IsUnlabelled'] = isUnlab
        
    bad = ids[isUnlab]
        
    return (len(bad)/len(ids), len(bad))

    
def get_clean_ids(ids, removeDecoy = True, decoyTag = None, removeUnlabelled = True, unlabelledTag = None):
    
    if(removeDecoy == True):
        isDecoy = ids["IsDecoy"]
    else:
        isDecoy = [False for _ in range(0, len(ids))]
    
    if(removeUnlabelled == True):
        isUnlab = check_unlabelled(ids, unlabelledTag)
    else:
        isUnlab = [False for _ in range(0, len(ids))]
       
    wanted = [d == False and u == False for d,u in zip(isDecoy, isUnlab)]
    removed = [ not w for w in wanted]
    
    wanted = ids[wanted].reset_index().drop('index', axis=1)
    removed = ids[removed].reset_index().drop('index', axis=1)
    
    return (wanted, removed)
    

#%% Standard Pipeline

def apply_thresholds(df, thresholds):
    newDF = df.copy()
    
    decoys = df[df['IsDecoy'] == True]
    targets = df[df['IsDecoy'] == False]
    
    totalRemovedCount = 0
    
    for feature in thresholds.keys():
        targetsBefore = len(targets)
        
        lowerOrHigher = thresholds[feature][0]
        thresholdValue = thresholds[feature][1]
        if lowerOrHigher == 'lower':
            targets = targets[targets[feature] < thresholdValue]
        elif lowerOrHigher == 'lowerOrEqual':
            targets = targets[targets[feature] <= thresholdValue]
        elif lowerOrHigher == 'lowerAbs':
            targets = targets[[True if abs(v) < thresholdValue else False for v in targets[feature]]]
        elif lowerOrHigher == 'higher':
            targets = targets[targets[feature] > thresholdValue]
        elif lowerOrHigher == 'higherOrEqual':
            targets = targets[targets[feature] >= thresholdValue]
        elif lowerOrHigher == 'higherAbs':
            targets = targets[[True if abs(v) > thresholdValue else False for v in targets[feature]]]
        else:
            raise Exception("Threshold type '{0}' not known.".format(lowerOrHigher))
            
        targetsAfter = len(targets)
        
        targets = targets.reset_index(drop = True)
        
        totalRemovedCount +=  targetsBefore - targetsAfter
    
        print('Applying {0} threshold of {1}. {2} targets removed'.format(feature, thresholdValue, targetsBefore - targetsAfter))
    
    # targets = targets[targets['DiffPPM'] < 15]
    # targets = targets[targets['XlinkxGroupedScore'] >= 8]
    # targets = targets[targets['MinDDPScore'] >= 0.06]
    
    newDF = pd.concat([targets,decoys])
    newDF = newDF.reset_index(drop = True)
    
    return newDF
    

def get_distinct_proteins(df, row1 = 'ProtLocus 1', row2 = 'ProtLocus 2', sep = ','):
    
    prots1 = df[row1]
    prots2 = df[row2]

    allProts = list(prots1)+list(prots2)
    allProts = [p.split(sep) for p in allProts]
    allProts = list(set(itertools.chain.from_iterable(allProts)))
       
    return allProts
    
    

        
#%%

def get_csms_from_ppis(csms, ppis):
    
    df = csms[[ c in list(ppis['PPI']) for c in csms['PPI']]]
    df = df.reset_index(drop=True)
    
    return df
        
def get_csms_per_file(csms, inter_only = False):
    dict1 = {}

    if(inter_only):
        n_csms = csms[csms['Inter']]
        n_csms = n_csms.reset_index(drop=True)
    else:
        n_csms = csms.copy()

    for i, row in n_csms.iterrows():
        f = row['FileName']
        
        if( f in dict1.keys() ):
            dict1[f]  += 1
        else:
            dict1[f] = 1
    
    return dict1
        
        
        
        
                
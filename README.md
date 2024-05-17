# Scout
Interactomics studies play a critical role in elucidating protein structures, functions, and interactions within complex cellular environments. Cross-linking mass spectrometry with cleavable cross-linking reagents (cXL-MS) has emerged as a powerful technique for large-scale interactomics analysis by identifying proximal amino acid pairs in protein samples. However, current computational cXL-MS tools face limitations in proteomic-scale studies, such as being too slow or generating excessive false positives, particularly at the protein-protein interactions level (PPIs). 

Here, we present **Scout**, a computational methodology that enables interactomic analysis by identifying mass spectra of peptides linked with cleavable cross-linking reagents. By leveraging machine learning techniques, Scout ensures a controlled false discovery rate (FDR) at multiple levels, including cross-linked spectrum matches, residue pairs, and PPIs. Our methodology offers an efficient and accurate solution for large-scale interactomics studies, addressing the existing computational challenges.

_<b>Please cite our paper:</b>_<br/>
_Clasen, MA, et al., [“Proteome-scale recombinant standards and a robust high-speed search engine to advance cross-linking MS-based interactomics”](https://doi.org/10.1101/2023.11.30.569448), bioRxiv, 2023._

# Equipment
## Hardware
- A computer with a minimum of 16 GB RAM and 4 computing cores is recommended.  However, the software can take advantage of superior configurations.

## Software
-	Windows 10 (64 bits) or later.
-	Python 3.10 or later.
-	The .NET Core 6 or later.

# Procedures

1. **Software installation:**<br/>
<i>For the original manuscript we used version [1.4.14](https://github.com/diogobor/Scout/releases/tag/1.4.14) for the entire data analysis.<br/>
To obtain the latest version of Scout, please download it [here](https://github.com/diogobor/Scout/releases/).</i><br/>

1. **Workflow:**<br/>
The workflow demonstrates how to perform a search using _Scout_. [Access it here](https://github.com/diogobor/Scout/#procedures).

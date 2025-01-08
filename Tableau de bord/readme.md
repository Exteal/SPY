
## Rapport sur le projet Spy Dashboard



### **Introduction**

Le projet **Spy Dashboard** vise à fournir une plateforme interactive et visuelle pour suivre les performances des joueurs dans divers scénarios de jeu. Développé en utilisant **Dash** (Python), cet outil s’inscrit dans une initiative pédagogique ou ludique, destinée à analyser les comportements et progrès des joueurs tout en identifiant leurs points forts et faibles. Le tableau de bord offre une interface utilisateur intuitive, combinée à des graphiques détaillés et des fonctionnalités de personnalisation.

Ce rapport met en lumière les fonctionnalités clés, les composantes techniques, ainsi que les points forts et les perspectives d’amélioration de ce projet.


### **Partie 1 : Fonctionnalités principales**

#### **1.1 Affichage des joueurs**
- **Classement des joueurs :**
  Les joueurs sont affichés dans une liste classée en fonction de leur score total. Les trois premiers sont représentés par des icônes distinctives.

- **Interactivité :**
Lorsqu'un joueur est sélectionné, ses scénarios et ses données spécifiques apparaissent dans la partie droite du tableau de bord.

#### **1.2 Gestion des scénarios**
Chaque joueur dispose d'une liste de scénarios joués ou non. 
-Les scénarios joués sont mis en évidence pour une meilleure lisibilité.
Lorsqu'un scénario est sélectionné, plusieurs informations sont affichées :
- **Niveaux terminés :** Les niveaux déjà accomplis par le joueur dans le scénario sélectionné.
- **Temps passé :** Calculé comme la différence entre le lancement et la complétion d’un niveau (si plusieurs tentatives existent, seule celle avec le meilleur score est retenue).
- **Score obtenu :** Accompagné d’un système d’évaluation en étoiles pour chaque niveau (1 à 3 étoiles).

#### **1.3 Visualisations interactives**
- **Graphique d’expertise du joueur :**
  Un graphique montre le pourcentage d’expertise dans deux domaines clés :
  - **Boucles (Loops) :** Proportion des niveaux impliquant des boucles achevés par le joueur.
  - **Conditions (Conditions) :** Proportion des niveaux utilisant des conditions terminés.

- **Diagramme circulaire :**
  Représente le taux moyen d’accomplissement pour le scénario sélectionné. Ce taux est calculé comme la moyenne des niveaux terminés par les joueurs, divisée par le nombre total de niveaux du scénario.

- **Graphique des temps moyens :**
  Affiche une barre horizontale pour chaque niveau d’un scénario, représentant le temps moyen passé par tous les joueurs sur ce niveau.



### **Partie 2 : Structure technique**

#### **2.1 Technologies utilisées**
Le projet s’appuie sur un ensemble de technologies modernes et robustes :
- **Dash :** Framework principal pour le développement de l’application web.
- **Pandas :** Pour l’analyse, la manipulation et l’agrégation des données issues des joueurs et des scénarios.
- **Plotly :** Génère des graphiques interactifs et attractifs.
- **HTML/CSS :** Personnalise l’apparence de l’interface et améliore l’expérience utilisateur.

#### **2.2 Organisation du projet**
1. **Chargement des données :**
   Les données des joueurs sont stockées dans des fichiers CSV telles que le temps de jeu, les scores, et les scénarios joués, qui sont traités et agrégés via des scripts Python.
   Un fichier modèle (model.csv) définit les caractéristiques des niveaux :

    - Présence de boucles ou de conditions (indiquées par des valeurs binaires : 1 ou 0).
    - Scores maximum et étoiles attribuées.
   
2. **Traitement des données :**
   - **Temps passé :** Une fonction calcule le temps passé sur chaque niveau en identifiant les événements "lancement" et "fin".
   La fonction 'calculate_time_spent' extrait le temps passé sur chaque niveau en croisant les événements "launched" (lancement du niveau) et "completed" (fin du niveau). Si un niveau est joué plusieurs fois, seule la tentative avec le meilleur score est considérée.
   - **Expertise :** Une fonction calcule le pourcentage d’expertise basé sur les niveaux impliquant des boucles et des conditions.
   La fonction 'calculate_expertise' mesure les compétences d’un joueur dans deux domaines : les boucles et les conditions. Elle compare les scores obtenus aux scores maximaux définis pour chaque niveau dans le fichier modèle.
   - **Classement :** Une fonction génère le classement des joueurs avec des icônes pour les trois premiers.
   La fonction 'ranking' génère un classement des joueurs basé sur leur score total. Les trois premiers joueurs reçoivent des icônes spécifiques (👑, 🥈, 🥉) pour les distinguer visuellement.

3. **Affichage dynamique :**
    - Les graphiques interactifs sont mis à jour dynamiquement grâce à des callbacks Dash, en fonction des sélections de l’utilisateur.
   - L’état sélectionné (joueur ou scénario) est géré par des composants Dash comme `dcc.Store` et les callbacks Python.



### **Partie 3 : Points clés et défis techniques**

#### **3.1 Points clés**
- **Facilité d’utilisation :** Une interface fluide et réactive, adaptée aux utilisateurs novices comme experts.
- **Visualisations intuitives :** Les graphiques offrent une compréhension rapide et claire des données.
- **Personnalisation dynamique :** L’utilisateur peut explorer les données spécifiques à chaque joueur et scénario.

#### **3.2 Défis techniques**
- La manipulation de fichiers volumineux nécessite des optimisations pour réduire les temps de calcul et le temps de chargement.



